using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Akeeba.Unarchiver
{
    class AbstractUnarchiver
    {
        /// <summary>
        /// The supported file extension of this unarchiver, e.g. "jpa". Must be set in the constructor or the class declaration.
        /// </summary>
        protected readonly string supportedExtension;

        /// <summary>
        /// Currently open input file stream
        /// </summary>
        protected FileStream propInputFile = null;

        /// <summary>
        /// The part number we are reading from
        /// </summary>
        protected int? currentPartNumber = null;

        /// <summary>
        /// Absolute path to the archive file
        /// </summary>
        private string propArchivePath = "";

        /// <summary>
        /// Number of total archive parts, including the final part (.jpa, .jps or .zip extension)
        /// </summary>
        private int? propParts = null;

        /// <summary>
        /// The absolute path to the archive file. Always assign the last part of a multipart archive (.jpa, .jps or .zip).
        /// </summary>
        public string archivePath {
            get
            {
                return propArchivePath;
            }

            set
            {
                if (!File.Exists(value))
                {
                    throw new FileNotFoundException();
                }

                string strExtension = Path.GetExtension(value);

                if (strExtension.ToUpper() != supportedExtension.ToUpper())
                {
                    throw new InvalidExtensionException();
                }

                propArchivePath = value;
            }
        }

        /// <summary>
        /// Total number of archive parts
        /// </summary>
        public int parts
        {
            get
            {
                if (!propParts.HasValue)
                {
                    try
                    {
                        discoverParts();
                    }
                    catch (Exception)
                    {
                        propParts = 0;
                    }
                }

                return (int) propParts;
            }
        }

        /// <summary>
        /// Returns the input file stream, or Nothing if we're at the EOF of the last part
        /// </summary>
        protected FileStream inputFile
        {
            get
            {
                if (propInputFile == null )
                {
                    /**
                     * No currently open file. Try to open the current part number. If it's null we open the first part. If it's out of
                     * range we throw an exception.
                     */
                    if (!currentPartNumber.HasValue)
                    {
                        currentPartNumber = 1;
                    }
                    else if ((currentPartNumber <= 0) || (currentPartNumber > parts))
                    {
                        throw new IndexOutOfRangeException();
                    }

                    propInputFile = new FileStream(getPartFilename(currentPartNumber), FileMode.Open);
                }
                else if (propInputFile.Position >= propInputFile.Length)
                {
                    // We have reached EOF. Open the next part file if applicable.
                    propInputFile.Dispose();
                    currentPartNumber += 1;

                    if (currentPartNumber <= parts)
                    {
                        propInputFile = new FileStream(getPartFilename(currentPartNumber), FileMode.Open);
                    }
                }

                return propInputFile;
            }
        }

        protected void discoverParts()
        {
            propParts = 0;

            if (!File.Exists(propArchivePath))
            {
                return;
            }

            string strExtension = Path.GetExtension(propArchivePath);
            string strNewFile = "";
            int currentPartNumber = 0;

            do
            {
                propParts += 1;
                currentPartNumber += 1;

                strNewFile = Path.ChangeExtension(propArchivePath, strExtension.Substring(0,1) + string.Format("{0:00}", currentPartNumber));
            } while (File.Exists(strNewFile));

        }
    }
}
