/*
 * Copyright (c) Microsoft Corporation. All rights reserved.
 */

'use strict';

const models = require('./index');

/**
 * Describes the basic properties for encoding a video.
 *
 * @extends models['Codec']
 */
class Video extends models['Codec'] {
  /**
   * Create a Video.
   * @member {moment.duration} [keyFrameInterval] Describes the distance
   * between one key frame and the next, thereby defining a GOP or group of
   * pictures. The value should be a non-zero integer in the range [1, .., 30],
   * specified in ISO 8601 format. The default is 2 seconds.
   * @member {string} [stretchMode] Describes the resizing mode - how the input
   * video will be resized to fit the desired output resolution(s). Default is
   * AutoSize. Possible values include: 'None', 'AutoSize', 'AutoFit'
   */
  constructor() {
    super();
  }

  /**
   * Defines the metadata of Video
   *
   * @returns {object} metadata of Video
   *
   */
  mapper() {
    return {
      required: false,
      serializedName: '#Microsoft.Media.Video',
      type: {
        name: 'Composite',
        polymorphicDiscriminator: {
          serializedName: '@odata.type',
          clientName: 'odatatype'
        },
        uberParent: 'Codec',
        className: 'Video',
        modelProperties: {
          label: {
            required: false,
            serializedName: 'label',
            type: {
              name: 'String'
            }
          },
          odatatype: {
            required: true,
            serializedName: '@odata\\.type',
            isPolymorphicDiscriminator: true,
            type: {
              name: 'String'
            }
          },
          keyFrameInterval: {
            required: false,
            serializedName: 'keyFrameInterval',
            type: {
              name: 'TimeSpan'
            }
          },
          stretchMode: {
            required: false,
            serializedName: 'stretchMode',
            type: {
              name: 'String'
            }
          }
        }
      }
    };
  }
}

module.exports = Video;
