﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop PSD FileType Plugin for Paint.NET
// http://psdplugin.codeplex.com/
//
// This software is provided under the MIT License:
//   Copyright (c) 2006-2007 Frank Blumenberg
//   Copyright (c) 2010-2016 Tao Yue
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using PhotoshopFile;

namespace PixelDotNet.Data.PhotoshopFileType
{
  public static class PsdLoad
  {
    public static Document Load(System.IO.Stream input)
    {
      // Load and decompress Photoshop file structures
      var loadContext = new LoadContext();
      var psdFile = new PsdFile(input, loadContext);

      // Multichannel images are loaded by processing each channel as a
      // grayscale layer.
      if (psdFile.ColorMode == PsdColorMode.Multichannel)
      {
        CreateLayersFromChannels(psdFile);
        psdFile.ColorMode = PsdColorMode.Grayscale;
      }

      // Convert into Paint.NET internal representation
      var document = new Document(psdFile.ColumnCount, psdFile.RowCount);

      if (psdFile.Layers.Count == 0)
      {
        psdFile.BaseLayer.CreateMissingChannels();
        var layer = Layer.CreateBackgroundLayer(psdFile.ColumnCount, psdFile.RowCount);
        ImageDecoderPdn.DecodeImage(layer, psdFile.BaseLayer);
        document.Layers.Add(layer);
      }
      else
      {
        psdFile.VerifyLayerSections();
        ApplyLayerSections(psdFile.Layers);

        var pdnLayers = psdFile.Layers.AsParallel().AsOrdered()
          .Select(psdLayer => psdLayer.DecodeToPdnLayer())
          .ToList();
        document.Layers.AddRange(pdnLayers);
      }

      return document;
    }

    internal static BitmapLayer DecodeToPdnLayer(
      this PhotoshopFile.Layer psdLayer)
    {
      var psdFile = psdLayer.PsdFile;
      psdLayer.CreateMissingChannels();

      var pdnLayer = new BitmapLayer(psdFile.ColumnCount, psdFile.RowCount);
      pdnLayer.Name = psdLayer.Name;
      pdnLayer.Opacity = psdLayer.Opacity;
      pdnLayer.Visible = psdLayer.Visible;
      ImageDecoderPdn.DecodeImage(pdnLayer, psdLayer);

      return pdnLayer;
    }

    /// <summary>
    /// Creates a layer for each channel in a multichannel image.
    /// </summary>
    private static void CreateLayersFromChannels(PsdFile psdFile)
    {
      if (psdFile.ColorMode != PsdColorMode.Multichannel)
        throw new Exception("Not a multichannel image.");
      if (psdFile.Layers.Count > 0)
        throw new PsdInvalidException("Multichannel image should not have layers.");

      // Get alpha channel names, preferably in Unicode.
      var alphaChannelNames = (AlphaChannelNames)psdFile.ImageResources
        .Get(ResourceID.AlphaChannelNames);
      var unicodeAlphaNames = (UnicodeAlphaNames)psdFile.ImageResources
        .Get(ResourceID.UnicodeAlphaNames);
      if ((alphaChannelNames == null) && (unicodeAlphaNames == null))
        throw new PsdInvalidException("No channel names found.");

      var channelNames = (unicodeAlphaNames != null)
        ? unicodeAlphaNames.ChannelNames
        : alphaChannelNames.ChannelNames;
      var channels = psdFile.BaseLayer.Channels;
      if (channels.Count > channelNames.Count)
        throw new PsdInvalidException("More channels than channel names.");

      // Channels are stored from top to bottom, but layers are stored from
      // bottom to top.
      for (int i = channels.Count - 1; i >= 0; i--)
      {
        var channel = channels[i];
        var channelName = channelNames[i];

        // Copy metadata over from base layer
        var layer = new PhotoshopFile.Layer(psdFile);
        layer.Rect = psdFile.BaseLayer.Rect;
        layer.Visible = true;
        layer.Masks = new MaskInfo();
        layer.BlendingRangesData = new BlendingRanges(layer);

        // We do not attempt to reconstruct the appearance of the image, but
        // only to provide access to the channels image data.
        layer.Name = channelName;
        layer.BlendModeKey = PsdBlendMode.Darken;
        layer.Opacity = 255;

        // Copy channel image data into the new grayscale layer
        var layerChannel = new Channel(0, layer);
        layerChannel.ImageCompression = channel.ImageCompression;
        layerChannel.ImageData = channel.ImageData;
        layer.Channels.Add(layerChannel);

        psdFile.Layers.Add(layer);
      }
    }

    /// <summary>
    /// Transform Photoshop's layer tree to Paint.NET's flat layer list.
    /// Indicate where layer sections begin and end, and hide all layers within
    /// hidden layer sections.
    /// </summary>
    private static void ApplyLayerSections(List<PhotoshopFile.Layer> layers)
    {
      // BUG: PsdPluginResources.GetString will always return English resource,
      // because Paint.NET does not set the CurrentUICulture when OnLoad is
      // called.  This situation should be resolved with Paint.NET 4.0, which
      // will provide an alternative mechanism to retrieve the UI language.

      // Cache layer section strings
      var beginSectionWrapper = ("LayersPalette_LayerGroupBegin");
      var endSectionWrapper = ("LayersPalette_LayerGroupEnd");
      
      // Track the depth of the topmost hidden section.  Any nested sections
      // will be hidden, whether or not they themselves have the flag set.
      int topHiddenSectionDepth = Int32.MaxValue;
      var layerSectionNames = new Stack<string>();

      // Layers are stored bottom-to-top, but layer sections are specified
      // top-to-bottom.
      foreach (var layer in Enumerable.Reverse(layers))
      {
        // Apply to all layers within the layer section, as well as the
        // closing layer.
        if (layerSectionNames.Count > topHiddenSectionDepth)
          layer.Visible = false;

        var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
          .SingleOrDefault(x => x is LayerSectionInfo);
        if (sectionInfo == null)
          continue;

        switch (sectionInfo.SectionType)
        {
          case LayerSectionType.OpenFolder:
          case LayerSectionType.ClosedFolder:
            // Start a new layer section
            if ((!layer.Visible) && (topHiddenSectionDepth == Int32.MaxValue))
              topHiddenSectionDepth = layerSectionNames.Count;
            layerSectionNames.Push(layer.Name);
            layer.Name = String.Format(beginSectionWrapper, layer.Name);
            break;

          case LayerSectionType.SectionDivider:
            // End the current layer section
            var layerSectionName = layerSectionNames.Pop();
            if (layerSectionNames.Count == topHiddenSectionDepth)
              topHiddenSectionDepth = Int32.MaxValue;
            layer.Name = String.Format(endSectionWrapper, layerSectionName);
            break;
        }
      }
    }

  }

}
