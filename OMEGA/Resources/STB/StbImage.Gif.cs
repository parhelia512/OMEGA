﻿namespace STB
{
    internal unsafe partial class StbImage
    {
        public static int stbi__gif_test_raw(stbi__context s)
        {
            var sz = 0;
            if (stbi__get8(s) != 'G' || stbi__get8(s) != 'I' || stbi__get8(s) != 'F' || stbi__get8(s) != '8')
                return 0;
            sz = stbi__get8(s);
            if (sz != '9' && sz != '7')
                return 0;
            if (stbi__get8(s) != 'a')
                return 0;
            return 1;
        }

        public static int stbi__gif_test(stbi__context s)
        {
            var r = stbi__gif_test_raw(s);
            stbi__rewind(s);
            return r;
        }

        public static int stbi__gif_header(stbi__context s, stbi__gif g, int* comp, int is_info)
        {
            byte version = 0;
            if (stbi__get8(s) != 'G' || stbi__get8(s) != 'I' || stbi__get8(s) != 'F' || stbi__get8(s) != '8')
                return stbi__err("not GIF");
            version = stbi__get8(s);
            if (version != '7' && version != '9')
                return stbi__err("not GIF");
            if (stbi__get8(s) != 'a')
                return stbi__err("not GIF");
            stbi__g_failure_reason = "";
            g.w = stbi__get16le(s);
            g.h = stbi__get16le(s);
            g.flags = stbi__get8(s);
            g.bgindex = stbi__get8(s);
            g.ratio = stbi__get8(s);
            g.transparent = -1;
            if (comp != null)
                *comp = 4;
            if (is_info != 0)
                return 1;
            if ((g.flags & 0x80) != 0)
                stbi__gif_parse_colortable(s, g.pal, 2 << (g.flags & 7), -1);
            return 1;
        }

        public static int stbi__gif_info_raw(stbi__context s, int* x, int* y, int* comp)
        {
            var g = new stbi__gif();
            if (stbi__gif_header(s, g, comp, 1) == 0)
            {
                stbi__rewind(s);
                return 0;
            }

            if (x != null)
                *x = g.w;
            if (y != null)
                *y = g.h;

            return 1;
        }

        public static void stbi__out_gif_code(stbi__gif g, ushort code)
        {
            byte* p;
            byte* c;
            var idx = 0;
            if (g.codes[code].prefix >= 0)
                stbi__out_gif_code(g, (ushort)g.codes[code].prefix);
            if (g.cur_y >= g.max_y)
                return;
            idx = g.cur_x + g.cur_y;
            p = &g._out_[idx];
            g.history[idx / 4] = 1;
            c = &g.color_table[g.codes[code].suffix * 4];
            if (c[3] > 128)
            {
                p[0] = c[2];
                p[1] = c[1];
                p[2] = c[0];
                p[3] = c[3];
            }

            g.cur_x += 4;
            if (g.cur_x >= g.max_x)
            {
                g.cur_x = g.start_x;
                g.cur_y += g.step;
                while (g.cur_y >= g.max_y && g.parse > 0)
                {
                    g.step = (1 << g.parse) * g.line_size;
                    g.cur_y = g.start_y + (g.step >> 1);
                    --g.parse;
                }
            }
        }

        public static byte* stbi__process_gif_raster(stbi__context s, stbi__gif g)
        {
            byte lzw_cs = 0;
            var len = 0;
            var init_code = 0;
            uint first = 0;
            var codesize = 0;
            var codemask = 0;
            var avail = 0;
            var oldcode = 0;
            var bits = 0;
            var valid_bits = 0;
            var clear = 0;
            stbi__gif_lzw* p;
            lzw_cs = stbi__get8(s);
            if (lzw_cs > 12)
                return null;
            clear = 1 << lzw_cs;
            first = 1;
            codesize = lzw_cs + 1;
            codemask = (1 << codesize) - 1;
            bits = 0;
            valid_bits = 0;
            for (init_code = 0; init_code < clear; init_code++)
            {
                g.codes[init_code].prefix = -1;
                g.codes[init_code].first = (byte)init_code;
                g.codes[init_code].suffix = (byte)init_code;
            }

            avail = clear + 2;
            oldcode = -1;
            len = 0;
            for (; ; )
                if (valid_bits < codesize)
                {
                    if (len == 0)
                    {
                        len = stbi__get8(s);
                        if (len == 0)
                            return g._out_;
                    }

                    --len;
                    bits |= stbi__get8(s) << valid_bits;
                    valid_bits += 8;
                }
                else
                {
                    var code = bits & codemask;
                    bits >>= codesize;
                    valid_bits -= codesize;
                    if (code == clear)
                    {
                        codesize = lzw_cs + 1;
                        codemask = (1 << codesize) - 1;
                        avail = clear + 2;
                        oldcode = -1;
                        first = 0;
                    }
                    else if (code == clear + 1)
                    {
                        stbi__skip(s, len);
                        while ((len = stbi__get8(s)) > 0)
                            stbi__skip(s, len);
                        return g._out_;
                    }
                    else if (code <= avail)
                    {
                        if (first != 0)
                            return (byte*)(ulong)(stbi__err("no clear code") != 0 ? (byte*)null : null);
                        if (oldcode >= 0)
                        {
                            p = g.codes + avail++;
                            if (avail > 8192)
                                return (byte*)(ulong)(stbi__err("too many codes") != 0 ? (byte*)null : null);
                            p->prefix = (short)oldcode;
                            p->first = g.codes[oldcode].first;
                            p->suffix = code == avail ? p->first : g.codes[code].first;
                        }
                        else if (code == avail)
                        {
                            return (byte*)(ulong)(stbi__err("illegal code in raster") != 0 ? (byte*)null : null);
                        }

                        stbi__out_gif_code(g, (ushort)code);
                        if ((avail & codemask) == 0 && avail <= 0x0FFF)
                        {
                            codesize++;
                            codemask = (1 << codesize) - 1;
                        }

                        oldcode = code;
                    }
                    else
                    {
                        return (byte*)(ulong)(stbi__err("illegal code in raster") != 0 ? (byte*)null : null);
                    }
                }
        }

        public static byte* stbi__gif_load_next(stbi__context s, stbi__gif g, int* comp, int req_comp, byte* two_back)
        {
            var dispose = 0;
            var first_frame = 0;
            var pi = 0;
            var pcount = 0;
            first_frame = 0;
            if (g._out_ == null)
            {
                if (stbi__gif_header(s, g, comp, 0) == 0)
                    return null;
                if (stbi__mad3sizes_valid(4, g.w, g.h, 0) == 0)
                    return (byte*)(ulong)(stbi__err("too large") != 0 ? (byte*)null : null);
                pcount = g.w * g.h;
                g._out_ = (byte*)stbi__malloc((ulong)(4 * pcount));
                g.background = (byte*)stbi__malloc((ulong)(4 * pcount));
                g.history = (byte*)stbi__malloc((ulong)pcount);
                if (g._out_ == null || g.background == null || g.history == null)
                    return (byte*)(ulong)(stbi__err("outofmem") != 0 ? (byte*)null : null);
                CRuntime.memset(g._out_, 0x00, (ulong)(4 * pcount));
                CRuntime.memset(g.background, 0x00, (ulong)(4 * pcount));
                CRuntime.memset(g.history, 0x00, (ulong)pcount);
                first_frame = 1;
            }
            else
            {
                dispose = (g.eflags & 0x1C) >> 2;
                pcount = g.w * g.h;
                if (dispose == 3 && two_back == null)
                    dispose = 2;
                if (dispose == 3)
                {
                    for (pi = 0; pi < pcount; ++pi)
                        if (g.history[pi] != 0)
                            CRuntime.memcpy(&g._out_[pi * 4], &two_back[pi * 4], (ulong)4);
                }
                else if (dispose == 2)
                {
                    for (pi = 0; pi < pcount; ++pi)
                        if (g.history[pi] != 0)
                            CRuntime.memcpy(&g._out_[pi * 4], &g.background[pi * 4], (ulong)4);
                }

                CRuntime.memcpy(g.background, g._out_, (ulong)(4 * g.w * g.h));
            }

            CRuntime.memset(g.history, 0x00, (ulong)(g.w * g.h));
            for (; ; )
            {
                var tag = (int)stbi__get8(s);
                switch (tag)
                {
                    case 0x2C:
                        {
                            var x = 0;
                            var y = 0;
                            var w = 0;
                            var h = 0;
                            byte* o;
                            x = stbi__get16le(s);
                            y = stbi__get16le(s);
                            w = stbi__get16le(s);
                            h = stbi__get16le(s);
                            if (x + w > g.w || y + h > g.h)
                                return (byte*)(ulong)(stbi__err("bad Image Descriptor") != 0 ? (byte*)null : null);
                            g.line_size = g.w * 4;
                            g.start_x = x * 4;
                            g.start_y = y * g.line_size;
                            g.max_x = g.start_x + w * 4;
                            g.max_y = g.start_y + h * g.line_size;
                            g.cur_x = g.start_x;
                            g.cur_y = g.start_y;
                            if (w == 0)
                                g.cur_y = g.max_y;
                            g.lflags = stbi__get8(s);
                            if ((g.lflags & 0x40) != 0)
                            {
                                g.step = 8 * g.line_size;
                                g.parse = 3;
                            }
                            else
                            {
                                g.step = g.line_size;
                                g.parse = 0;
                            }

                            if ((g.lflags & 0x80) != 0)
                            {
                                stbi__gif_parse_colortable(s, g.lpal, 2 << (g.lflags & 7),
                                    (g.eflags & 0x01) != 0 ? g.transparent : -1);
                                g.color_table = g.lpal;
                            }
                            else if ((g.flags & 0x80) != 0)
                            {
                                g.color_table = g.pal;
                            }
                            else
                            {
                                return (byte*)(ulong)(stbi__err("missing color table") != 0 ? (byte*)null : null);
                            }

                            o = stbi__process_gif_raster(s, g);
                            if (o == null)
                                return null;
                            pcount = g.w * g.h;
                            if (first_frame != 0 && g.bgindex > 0)
                                for (pi = 0; pi < pcount; ++pi)
                                    if (g.history[pi] == 0)
                                    {
                                        g.pal[g.bgindex * 4 + 3] = 255;
                                        CRuntime.memcpy(&g._out_[pi * 4], &g.pal[g.bgindex], (ulong)4);
                                    }

                            return o;
                        }
                    case 0x21:
                        {
                            var len = 0;
                            var ext = (int)stbi__get8(s);
                            if (ext == 0xF9)
                            {
                                len = stbi__get8(s);
                                if (len == 4)
                                {
                                    g.eflags = stbi__get8(s);
                                    g.delay = 10 * stbi__get16le(s);
                                    if (g.transparent >= 0)
                                        g.pal[g.transparent * 4 + 3] = 255;
                                    if ((g.eflags & 0x01) != 0)
                                    {
                                        g.transparent = stbi__get8(s);
                                        if (g.transparent >= 0)
                                            g.pal[g.transparent * 4 + 3] = 0;
                                    }
                                    else
                                    {
                                        stbi__skip(s, 1);
                                        g.transparent = -1;
                                    }
                                }
                                else
                                {
                                    stbi__skip(s, len);
                                    break;
                                }
                            }

                            while ((len = stbi__get8(s)) != 0)
                                stbi__skip(s, len);
                            break;
                        }
                    case 0x3B:
                        return null;
                    default:
                        return (byte*)(ulong)(stbi__err("unknown code") != 0 ? (byte*)null : null);
                }
            }
        }

        public static void* stbi__load_gif_main(stbi__context s, int** delays, int* x, int* y, int* z, int* comp,
            int req_comp)
        {
            if (stbi__gif_test(s) != 0)
            {
                var layers = 0;
                byte* u = null;
                byte* _out_ = null;
                byte* two_back = null;
                var g = new stbi__gif();
                var stride = 0;
                if (delays != null)
                    *delays = null;
                do
                {
                    u = stbi__gif_load_next(s, g, comp, req_comp, two_back);
                    if (u != null)
                    {
                        *x = g.w;
                        *y = g.h;
                        ++layers;
                        stride = g.w * g.h * 4;
                        if (_out_ != null)
                        {
                            _out_ = (byte*)CRuntime.realloc(_out_, (ulong)(layers * stride));
                            if (delays != null)
                                *delays = (int*)CRuntime.realloc(*delays, (ulong)(sizeof(int) * layers));
                        }
                        else
                        {
                            _out_ = (byte*)stbi__malloc((ulong)(layers * stride));
                            if (delays != null)
                                *delays = (int*)stbi__malloc((ulong)(layers * sizeof(int)));
                        }

                        CRuntime.memcpy(_out_ + (layers - 1) * stride, u, (ulong)stride);
                        if (layers >= 2)
                            two_back = _out_ - 2 * stride;
                        if (delays != null)
                            (*delays)[layers - 1U] = g.delay;
                    }
                } while (u != null);

                CRuntime.free(g._out_);
                CRuntime.free(g.history);
                CRuntime.free(g.background);
                if (req_comp != 0 && req_comp != 4)
                    _out_ = stbi__convert_format(_out_, 4, req_comp, (uint)(layers * g.w), (uint)g.h);
                *z = layers;
                return _out_;
            }

            return (byte*)(ulong)(stbi__err("not GIF") != 0 ? (byte*)null : null);
        }

        public static void* stbi__gif_load(stbi__context s, int* x, int* y, int* comp, int req_comp,
            stbi__result_info* ri)
        {
            byte* u = null;
            var g = new stbi__gif();

            u = stbi__gif_load_next(s, g, comp, req_comp, null);
            if (u != null)
            {
                *x = g.w;
                *y = g.h;
                if (req_comp != 0 && req_comp != 4)
                    u = stbi__convert_format(u, 4, req_comp, (uint)g.w, (uint)g.h);
            }
            else if (g._out_ != null)
            {
                CRuntime.free(g._out_);
            }

            CRuntime.free(g.history);
            CRuntime.free(g.background);
            return u;
        }

        public static int stbi__gif_info(stbi__context s, int* x, int* y, int* comp)
        {
            return stbi__gif_info_raw(s, x, y, comp);
        }
    }
}
