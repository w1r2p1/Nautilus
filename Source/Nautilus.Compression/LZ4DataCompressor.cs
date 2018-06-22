﻿//--------------------------------------------------------------------------------------------------
// <copyright file="LZ4DataCompressor.cs" company="Nautech Systems Pty Ltd">
//  Copyright (C) 2015-2018 Nautech Systems Pty Ltd. All rights reserved.
//  The use of this source code is governed by the license as found in the LICENSE.txt file.
//  http://www.nautechsystems.net
// </copyright>
//--------------------------------------------------------------------------------------------------

namespace Nautilus.Compression
{
    using System.Text;
    using LZ4;
    using Nautilus.Core.Annotations;
    using Nautilus.Core.Validation;
    using Nautilus.Database.Interfaces;

    /// <summary>
    /// Implements the LZ4 algorithm for compression and decompression of UTF8 encoded
    /// <see cref="byte"/> arrays.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class LZ4DataCompressor : IDataCompressor
    {
        private readonly bool isCompressionOn;

        /// <summary>
        /// Initializes a new instance of the <see cref="LZ4DataCompressor"/> class.
        /// </summary>
        /// <param name="isCompressionOn"></param>
        public LZ4DataCompressor(bool isCompressionOn)
        {
            this.isCompressionOn = isCompressionOn;
        }

        /// <summary>
        /// Returns a compressed <see cref="byte"/> array from the given UTF8 <see cref="string"/>.
        /// </summary>
        /// <param name="stringToCompress">The string to compress.</param>
        /// <returns>A compressed <see cref="byte"/> array.</returns>
        /// <exception cref="ValidationException">Throws if the string argument is null, empty or white space.</exception>
        public byte[] Write(string stringToCompress)
        {
            Validate.NotNull(stringToCompress, nameof(stringToCompress));

            return this.isCompressionOn
                 ? LZ4Codec.Wrap(Encoding.UTF8.GetBytes(stringToCompress))
                 : Encoding.UTF8.GetBytes(stringToCompress);
        }

        /// <summary>
        /// Returns a decompressed <see cref="string"/> from the given UTF8 <see cref="byte"/> array.
        /// </summary>
        /// <param name="bytesToDecompress">The bytes to decompress.</param>
        /// <returns>A decompressed <see cref="string"/>.</returns>
        /// <exception cref="ValidationException">Throws if the argument is null.</exception>
        public string Read(byte[] bytesToDecompress)
        {
            Validate.NotNull(bytesToDecompress, nameof(bytesToDecompress));

            return this.isCompressionOn
                 ? Encoding.UTF8.GetString(LZ4Codec.Unwrap(bytesToDecompress))
                 : Encoding.UTF8.GetString(bytesToDecompress);
        }

        /// <summary>
        /// Returns a decompressed <see cref="string"/> from the given UTF8 <see cref="byte"/> array.
        /// </summary>
        /// <param name="byteArraysToDecompress">The bytes to decompress.</param>
        /// <returns>A decompressed <see cref="string"/>.</returns>
        /// <exception cref="ValidationException">Throws if the argument is null.</exception>
        [PerformanceOptimized]
        public string[] Read(byte[][] byteArraysToDecompress)
        {
            Validate.NotNull(byteArraysToDecompress, nameof(byteArraysToDecompress));

            var stringArray = new string[byteArraysToDecompress.Length];

            if (this.isCompressionOn)
            {
                for (var i = 0; i < stringArray.Length; i++)
                {
                    stringArray[i] = Encoding.UTF8.GetString(byteArraysToDecompress[i]);
                }
            }
            else
            {
                for (var i = 0; i < stringArray.Length; i++)
                {
                    stringArray[i] = Encoding.UTF8.GetString(LZ4Codec.Unwrap(byteArraysToDecompress[i]));
                }
            }

            return stringArray;
        }
    }
}