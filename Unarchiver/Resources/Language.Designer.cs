﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Akeeba.Unarchiver.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Language {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Language() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Akeeba.Unarchiver.Resources.Language", typeof(Language).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The folder {0} does not exist.
        /// </summary>
        internal static string ERR_DATAWRITER_DIRECTORY_NOT_FOUND {
            get {
                return ResourceManager.GetString("ERR_DATAWRITER_DIRECTORY_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Central Directory file header data in part #{0}, offset {1}. The archive file is corrupt..
        /// </summary>
        internal static string ERR_FORMAT_INVALID_CD_HEADER_AT_POSITION {
            get {
                return ResourceManager.GetString("ERR_FORMAT_INVALID_CD_HEADER_AT_POSITION", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown compression method detected. The archive file is corrupt..
        /// </summary>
        internal static string ERR_FORMAT_INVALID_COMPRESSION_METHOD {
            get {
                return ResourceManager.GetString("ERR_FORMAT_INVALID_COMPRESSION_METHOD", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid entity type in the archive file. The archive file is corrupt..
        /// </summary>
        internal static string ERR_FORMAT_INVALID_ENTITY_TYPE {
            get {
                return ResourceManager.GetString("ERR_FORMAT_INVALID_ENTITY_TYPE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid optional file header data in part #{0}, offset {1}. The archive file is corrupt..
        /// </summary>
        internal static string ERR_FORMAT_INVALID_EXTRA_HEADER_AT_POSITION {
            get {
                return ResourceManager.GetString("ERR_FORMAT_INVALID_EXTRA_HEADER_AT_POSITION", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This file does not seem to be a {0} archive..
        /// </summary>
        internal static string ERR_FORMAT_INVALID_FILE_TYPE_SIGNATURE {
            get {
                return ResourceManager.GetString("ERR_FORMAT_INVALID_FILE_TYPE_SIGNATURE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid file header data in part #{0}, offset {1}. The archive file is corrupt..
        /// </summary>
        internal static string ERR_FORMAT_INVALID_HEADER_AT_POSITION {
            get {
                return ResourceManager.GetString("ERR_FORMAT_INVALID_HEADER_AT_POSITION", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown extra header signature in archive. Either the archive is corrupt or you need a newer version of this application..
        /// </summary>
        internal static string ERR_FORMAT_INVALID_JPA_EXTRA_HEADER {
            get {
                return ResourceManager.GetString("ERR_FORMAT_INVALID_JPA_EXTRA_HEADER", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid size of decrypted data. Expected {0} bytes, got {1} bytes..
        /// </summary>
        internal static string ERR_FORMAT_JPS_DECRYPTED_SIZE_DIFFERENCE {
            get {
                return ResourceManager.GetString("ERR_FORMAT_JPS_DECRYPTED_SIZE_DIFFERENCE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not decrypt data..
        /// </summary>
        internal static string ERR_FORMAT_JPS_DECRYPTION_FAILURE {
            get {
                return ResourceManager.GetString("ERR_FORMAT_JPS_DECRYPTION_FAILURE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify the encryption password to extract a JPS archive..
        /// </summary>
        internal static string ERR_FORMAT_JPS_EMPTY_PASSWORD {
            get {
                return ResourceManager.GetString("ERR_FORMAT_JPS_EMPTY_PASSWORD", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Archive files of the JPS format version {0}.{1} are not allowed to have extra header data..
        /// </summary>
        internal static string ERR_FORMAT_JPS_INVALID_EXTRA_HEADER_FOR_VERSION {
            get {
                return ResourceManager.GetString("ERR_FORMAT_JPS_INVALID_EXTRA_HEADER_FOR_VERSION", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to I do not know how to extract JPS format version {0}.{1} archive files..
        /// </summary>
        internal static string ERR_FORMAT_JPS_INVALID_VERSION {
            get {
                return ResourceManager.GetString("ERR_FORMAT_JPS_INVALID_VERSION", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Can not find the ZIP file&apos;s End of Central Directory record. The archive is corrupt, truncated or it is missing one or more part files..
        /// </summary>
        internal static string ERR_FORMAT_ZIP_EOCD_NOT_FOUND {
            get {
                return ResourceManager.GetString("ERR_FORMAT_ZIP_EOCD_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file {0} does not exist..
        /// </summary>
        internal static string ERR_UNARCHIVER_FILE_NOT_FOUND {
            get {
                return ResourceManager.GetString("ERR_UNARCHIVER_FILE_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File {0} doesn&apos;t have an extension valid for {1} archives..
        /// </summary>
        internal static string ERR_UNARCHIVER_INVALID_EXTENSION {
            get {
                return ResourceManager.GetString("ERR_UNARCHIVER_INVALID_EXTENSION", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Part number {0} is out of range. The archive only has {1} parts in total..
        /// </summary>
        internal static string ERR_UNARCHIVER_PART_NUMBER_OUT_OF_RANGE {
            get {
                return ResourceManager.GetString("ERR_UNARCHIVER_PART_NUMBER_OUT_OF_RANGE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to I don&apos;t know how to extract {0} files..
        /// </summary>
        internal static string ERR_UNARCHIVER_UNKNOWN_EXTENSION {
            get {
                return ResourceManager.GetString("ERR_UNARCHIVER_UNKNOWN_EXTENSION", resourceCulture);
            }
        }
    }
}