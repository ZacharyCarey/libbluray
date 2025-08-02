using libbluray.bdnav;
using libbluray.disc;
using libbluray.file;
using libbluray.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace libbluray.bdnav
{
    /// <summary>
    /// Thumbnail path and resolution
    /// </summary>
    public struct META_THUMBNAIL
    {
        /// <summary>
        /// Path to thumbnail image (relative to disc root)
        /// </summary>
        public string path;

        /// <summary>
        /// Thumbnail width
        /// </summary>
        public UInt32 xres;

        /// <summary>
        /// Thumbnail height
        /// </summary>
        public UInt32 yres;

        public META_THUMBNAIL() { }
    }

    /// <summary>
    /// Title name
    /// </summary>
    public struct META_TITLE
    {
        /// <summary>
        /// Title number (from disc index)
        /// </summary>
        public UInt32 title_number;
        public string title_name;

        public META_TITLE() { }
    }

    /// <summary>
    /// DL (Disc Library) metadata entry
    /// </summary>
    public struct META_DL
    {
        /// <summary>
        /// Language used in this metadata entry
        /// 3 bytes
        /// </summary>
        public string language_code;

        /// <summary>
        /// Source file (relative to disc root)
        /// </summary>
        public string filename;

        /// <summary>
        /// Disc name
        /// </summary>
        public string di_name;

        /// <summary>
        /// Alternative name
        /// </summary>
        public string di_alternative;

        /// <summary>
        /// Number of discs in original volume or collection
        /// </summary>
        public byte di_num_sets;

        /// <summary>
        /// Sequence order of the disc from an original collection
        /// </summary>
        public byte di_set_number;

        /// <summary>
        /// Number of title entries
        /// </summary>
        public UInt32 toc_count;

        /// <summary>
        /// Title data
        /// </summary>
        public Ref<META_TITLE> toc_entries = new();

        /// <summary>
        /// Number of thumbnails
        /// </summary>
        public byte thumb_count;

        /// <summary>
        /// Thumbnail data
        /// </summary>
        public Ref<META_THUMBNAIL> thumbnails = new();

        public META_DL() { }
    }

    internal struct META_TN
    {
        /// <summary>
        /// 3 bytes
        /// </summary>
        public string language_code;
        public string filename;

        public UInt32 playlist;
        public UInt32 num_chapter;
        public string[] chapter_name;

        public META_TN() { }
    }

    internal struct META_ROOT
    {
        public byte dl_count;
        public Ref<META_DL> dl_entries = new();

        public uint tn_count;
        public Ref<META_TN> tn_entries = new();

        public META_ROOT() { }

    }

    internal static class MetaParse
    {
        private const string DEFAULT_LANGUAGE = "eng";
        private const UInt32 MAX_META_FILE_SIZE = 0xfffff;

        static void _parseManifestNode(XmlNode a_node, Ref<META_DL> disclib)
        {
            foreach (XmlNode cur_node in a_node.ChildNodes)
            {
                if (cur_node is XmlElement element)
                {
                    if (element.ParentNode?.LocalName == "title")
                    {
                        if (element.LocalName == "name")
                        {
                            disclib.Value.di_name = element.InnerText ?? "";
                        }
                        if (element.LocalName == "alternative")
                        {
                            disclib.Value.di_alternative = element.InnerText ?? "";
                        }
                        if (element.LocalName == "numSets")
                        {
                            disclib.Value.di_num_sets = byte.Parse(element.InnerText ?? "0");
                        }
                        if (element.LocalName == "setNumber")
                        {
                            disclib.Value.di_set_number = byte.Parse(element.InnerText ?? "0");
                        }
                    }
                    else if (element.ParentNode?.LocalName == "tableOfContents")
                    {
                        if (element.LocalName == "titleName" && element.HasAttribute("titleNumber"))
                        {
                            Ref<META_TITLE> new_entries = disclib.Value.toc_entries.Reallocate(disclib.Value.toc_count + 1);
                            if (new_entries)
                            {
                                uint i = disclib.Value.toc_count;
                                disclib.Value.toc_count++;
                                disclib.Value.toc_entries = new_entries;
                                disclib.Value.toc_entries[i].title_number = uint.Parse(element.GetAttribute("titleNumber"));
                                disclib.Value.toc_entries[i].title_name = element.InnerText ?? "";
                            }
                        }
                    }
                    else if (element.ParentNode?.LocalName == "description")
                    {
                        if (element.LocalName == "thumbnail" && element.HasAttribute("href"))
                        {
                            Ref<META_THUMBNAIL> new_thumbnails = disclib.Value.thumbnails.Reallocate(disclib.Value.thumb_count + 1u);
                            if (new_thumbnails)
                            {
                                byte i = disclib.Value.thumb_count;
                                disclib.Value.thumb_count++;
                                disclib.Value.thumbnails = new_thumbnails;
                                disclib.Value.thumbnails[i].path = element.GetAttribute("href");
                                disclib.Value.thumbnails[i].xres = disclib.Value.thumbnails[i].yres = 0;
                                if (element.HasAttribute("size"))
                                {
                                    var match = Regex.Match(element.GetAttribute("size"), @"^\s*(\d+)x(\d+)\s*$", RegexOptions.IgnoreCase);
                                    if (match.Success)
                                    {
                                        disclib.Value.thumbnails[i].xres = uint.Parse(match.Groups[1].Value);
                                        disclib.Value.thumbnails[i].yres = uint.Parse(match.Groups[2].Value);
                                    }
                                }
                            }
                        }
                    }
                }
                _parseManifestNode(cur_node, disclib);
            }
        }

        static void _parseTnManifestNode(XmlNode a_node, Ref<META_TN> disclib)
        {
            foreach (XmlNode cur_node in a_node.ChildNodes)
            {
                if (cur_node is XmlElement element)
                {
                    if (element.ParentNode?.Name == "chapters")
                    {
                        if (element.Name == "name")
                        {
                            string[] new_entries = new string[disclib.Value.num_chapter + 1];
                            Array.Copy(disclib.Value.chapter_name, new_entries, disclib.Value.num_chapter);

                            uint i = disclib.Value.num_chapter;
                            disclib.Value.num_chapter++;
                            disclib.Value.chapter_name = new_entries;
                            disclib.Value.chapter_name[i] = element.Value;
                        }
                    }
                }
                _parseTnManifestNode(cur_node, disclib);
            }
        }

        static void _findMetaXMLfiles(Ref<META_ROOT> meta, BD_DISC disc)
        {
            BD_DIR_H? dir;
            BD_DIRENT ent = new();
            int res;

            dir = disc.disc_open_dir(Path.Combine("BDMV", "META", "DL"));
            if (dir == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"Failed to open meta dir BDMV/META/DL/");
            }
            else
            {

                for (res = dir.dir_read(out ent); res == 0; res = dir.dir_read(out ent))
                {
                    if (ent.d_name[0] == '.')
                        continue;
                    else if (ent.d_name.ToLower().StartsWith("bdmt_") && ent.d_name.Length == 12)
                    {
                        Ref<META_DL> new_dl_entries = meta.Value.dl_entries.Reallocate(meta.Value.dl_count + 1u);
                        if (new_dl_entries)
                        {
                            byte i = meta.Value.dl_count;
                            meta.Value.dl_count++;
                            meta.Value.dl_entries = new_dl_entries;
                            meta.Value.dl_entries[i] = new();

                            meta.Value.dl_entries[i].filename = ent.d_name;
                            meta.Value.dl_entries[i].language_code = ent.d_name[5..8].ToLower();
                        }
                    }
                }
                dir.dir_close();
            }

            dir = disc.disc_open_dir(Path.Combine("BDMV", "META", "TN"));
            if (dir == null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DIR, "Failed to open meta dir BDMV/META/TN/");
            }
            else
            {
                for (res = dir.dir_read(out ent); res == 0; res = dir.dir_read(out ent))
                {
                    if (ent.d_name.ToLower().StartsWith("tnmt_") && ent.d_name.Length == 18)
                    {
                        Ref<META_TN> new_tn_entries = meta.Value.tn_entries.Reallocate(meta.Value.tn_count + 1);
                        if (new_tn_entries)
                        {
                            uint i = meta.Value.tn_count;
                            meta.Value.tn_count++;
                            meta.Value.tn_entries = new_tn_entries;
                            meta.Value.tn_entries[i] = new();

                            meta.Value.tn_entries[i].filename = ent.d_name;
                            meta.Value.tn_entries[i].language_code = ent.d_name[5..8].ToLower();
                            meta.Value.tn_entries[i].playlist = uint.Parse(ent.d_name[9..]);
                        }
                    }
                }
                dir.dir_close();
            }
        }

        internal static Ref<META_ROOT> meta_parse(BD_DISC disc)
        {
            Ref<META_ROOT> root = Ref<META_ROOT>.Allocate();
            uint i;

            root.Value.dl_count = 0;
            _findMetaXMLfiles(root, disc);

            for (i = 0; i < root.Value.dl_count; i++)
            {
                Ref<byte> data;
                UInt64 size;
                size = disc.disc_read_file(Path.Combine("BDMV", "META", "DL"),
                                      root.Value.dl_entries[i].filename,
                                      out data);
                if (!data || size == 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"Failed to read BDMV/META/DL/{root.Value.dl_entries[i].filename}");
                }
                else
                {
                    XmlDocument doc = new XmlDocument();
                    using (MemoryStream ms = data.AsStream())
                    {
                        try
                        {
                            doc.Load(ms);
                        }
                        catch (Exception e)
                        {
                            doc = null;
                        }
                    }

                    if (doc == null)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"Failed to parse BDMV/META/DL/{root.Value.dl_entries[i].filename}");
                    }
                    else
                    {
                        XmlNode? root_element = doc;
                        root.Value.dl_entries[i].di_name = root.Value.dl_entries[i].di_alternative = null;
                        root.Value.dl_entries[i].di_num_sets = root.Value.dl_entries[i].di_set_number = 0;
                        root.Value.dl_entries[i].toc_count = root.Value.dl_entries[i].thumb_count = 0;
                        root.Value.dl_entries[i].toc_entries = Ref<META_TITLE>.Null;
                        root.Value.dl_entries[i].thumbnails = Ref<META_THUMBNAIL>.Null;
                        _parseManifestNode(root_element, root.Value.dl_entries.AtIndex(i));
                    }
                    data.Free();
                }
            }

            for (i = 0; i < root.Value.tn_count; i++)
            {
                Ref<byte> data;
                UInt64 size;
                size = disc.disc_read_file(Path.Combine("BDMV", "META", "TN"),
                                      root.Value.tn_entries[i].filename,
                                      out data);
                if (!data || size == 0)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"Failed to read BDMV/META/TN/{root.Value.tn_entries[i].filename}");
                }
                else
                {
                    XmlDocument doc = new XmlDocument();
                    using (MemoryStream ms = data.AsStream())
                    {
                        try
                        {
                            doc.Load(ms);
                        }
                        catch (Exception e)
                        {
                            doc = null;
                        }
                    }

                    if (doc == null)
                    {
                        Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"Failed to parse BDMV/META/TN/{root.Value.tn_entries[i].filename}");
                    }
                    else
                    {
                        XmlNode? root_element = doc;
                        _parseTnManifestNode(root_element, root.Value.tn_entries.AtIndex(i));
                    }
                    data.Free();
                }
            }

            return root;
        }

        internal static Ref<META_DL> meta_get(Ref<META_ROOT> meta_root, string? language_code)
        {
            uint i;

            if (meta_root == null || meta_root.Value.dl_count == 0)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DIR, "meta_get not possible, no info available!");
                return Ref<META_DL>.Null;
            }

            if (language_code != null)
            {
                for (i = 0; i < meta_root.Value.dl_count; i++)
                {
                    if (language_code == meta_root.Value.dl_entries[i].language_code)
                    {
                        return meta_root.Value.dl_entries.AtIndex(i);
                    }
                }
                Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"requested disclib language '{language_code}' not found");
            }

            for (i = 0; i < meta_root.Value.dl_count; i++)
            {
                if (DEFAULT_LANGUAGE == meta_root.Value.dl_entries[i].language_code)
                {
                    Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"using default disclib language '{DEFAULT_LANGUAGE}'");
                    return meta_root.Value.dl_entries.AtIndex(i);
                }
            }

            Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"requested disclib language '{language_code}' or default '{DEFAULT_LANGUAGE}' not found, using '{meta_root.Value.dl_entries[0].language_code}' instead");
            return meta_root.Value.dl_entries.AtIndex(0);
        }

        internal static Ref<META_TN> meta_get_tn(Ref<META_ROOT> meta_root, string language_code, uint playlist)
        {
            uint i;
            Ref<META_TN> tn_default = Ref<META_TN>.Null, tn_first = Ref<META_TN>.Null;

            if (meta_root.Value.tn_count == 0)
            {
                return Ref<META_TN>.Null;
            }

            for (i = 0; i < meta_root.Value.tn_count; i++)
            {
                if (meta_root.Value.tn_entries[i].playlist == playlist)
                {
                    if (language_code != null && language_code == meta_root.Value.tn_entries[i].language_code)
                    {
                        return meta_root.Value.tn_entries.AtIndex(i);
                    }
                    if (DEFAULT_LANGUAGE == meta_root.Value.tn_entries[i].language_code)
                    {
                        tn_default = meta_root.Value.tn_entries.AtIndex(i);
                    }
                    if (!tn_first)
                    {
                        tn_first = meta_root.Value.tn_entries.AtIndex(i);
                    }
                }
            }

            if (tn_default != null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"Requested disclib language '{language_code}' not found, using default language '{DEFAULT_LANGUAGE}'");
                return tn_default;
            }
            if (tn_first != null)
            {
                Logging.bd_debug(DebugMaskEnum.DBG_DIR, $"Requested disclib language '{language_code}' or default '{DEFAULT_LANGUAGE}' not found, using '{tn_first.Value.language_code}' instead");
                return tn_first;
            }
            return Ref<META_TN>.Null;
        }

        internal static void meta_free(ref Ref<META_ROOT> p)
        {
            if (p)
            {
                byte i;
                for (i = 0; i < p.Value.dl_count; i++)
                {
                    UInt32 t;
                    for (t = 0; t < p.Value.dl_entries[i].toc_count; t++)
                    {
                        p.Value.dl_entries[i].toc_entries[t].title_name = null;
                    }
                    for (t = 0; t < p.Value.dl_entries[i].thumb_count; t++)
                    {
                        p.Value.dl_entries[i].thumbnails[t].path = null;
                    }
                    p.Value.dl_entries[i].toc_entries.Free();
                    p.Value.dl_entries[i].thumbnails.Free();
                    p.Value.dl_entries[i].filename = null;
                    p.Value.dl_entries[i].di_name = null;
                    p.Value.dl_entries[i].di_alternative = null;
                }
                p.Value.dl_entries.Free();

                for (i = 0; i < p.Value.tn_count; i++)
                {
                    UInt32 c;
                    for (c = 0; c < p.Value.tn_entries[i].num_chapter; c++)
                    {
                        p.Value.tn_entries[i].chapter_name[c] = null;
                    }
                    p.Value.tn_entries[i].chapter_name = null;
                    p.Value.tn_entries[i].filename = null;
                }
                p.Value.tn_entries.Free();

                p.Free();
            }
        }
    }
}
