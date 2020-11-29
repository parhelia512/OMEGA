﻿using System;
using System.Runtime.InteropServices;

namespace OMEGA
{
    public struct Quad
    {
        public VertexPositionColorTexture V0;
        public VertexPositionColorTexture V1;
        public VertexPositionColorTexture V2;
        public VertexPositionColorTexture V3;

        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Quad));

        public float Width => Calc.Abs(V1.X - V0.X);
        public float Height => Calc.Abs(V2.Y - V1.Y);

      

        public Quad(Texture2D texture, RectF src_rect = default, RectF dest_rect = default)
        {
            this.V0 = new VertexPositionColorTexture();
            this.V1 = new VertexPositionColorTexture();
            this.V2 = new VertexPositionColorTexture();
            this.V3 = new VertexPositionColorTexture();

            if (texture == null) return;

            float ax, ay, bx, by;

            float dest_x1, dest_y1, dest_x2, dest_y2;
            
            if (src_rect.IsEmpty)
            {
                src_rect = RectF.FromBox(0, 0, texture.Width, texture.Height);
                
                ax = 0;
                ay = 0;
                bx = 1;
                by = 1;
            }
            else
            {
                float inv_tex_w = 1.0f / texture.Width;
                float inv_tex_h = 1.0f / texture.Height;
                
                ax = src_rect.X1 * inv_tex_w;
                ay = src_rect.Y1 * inv_tex_h;
                bx = src_rect.X2 * inv_tex_w;
                by = src_rect.Y2 * inv_tex_h;
            }

            if (dest_rect.IsEmpty)
            {
                dest_x1 = 0;
                dest_y1 = 0;
                dest_x2 = src_rect.Width;
                dest_y2 = src_rect.Height;
            }
            else
            {
                dest_x1 = dest_rect.X1;
                dest_y1 = dest_rect.Y1;
                dest_x2 = dest_rect.X2;
                dest_y2 = dest_rect.Y2;
            }
            
            this.V0.X = dest_x1;
            this.V0.Y = dest_y1;
            this.V0.Tx = (short)ax;
            this.V0.Ty = (short)ay;
            this.V0.Col = 0xFFFFFFFF;
            
            this.V1.X = dest_x2;
            this.V1.Y = dest_y1;
            this.V1.Tx = (short)bx;
            this.V1.Ty = (short)ay;
            this.V1.Col = 0xFFFFFFFF;
            
            this.V2.X = dest_x2;
            this.V2.Y = dest_y2;
            this.V2.Tx = (short)bx;
            this.V2.Ty = (short)by;
            this.V2.Col = 0xFFFFFFFF;
            
            this.V3.X = dest_x1;
            this.V3.Y = dest_y2;
            this.V3.Tx = (short)ax;
            this.V3.Ty = (short)by;
            this.V3.Col = 0xFFFFFFFF;
        }

        public void SetRect(Rect rect)
        {
            V0.X = rect.X1;
            V0.Y = rect.Y1;
            V1.X = rect.X2;
            V1.Y = rect.Y1;
            V2.X = rect.X2;
            V2.Y = rect.Y2;
            V3.X = rect.X1;
            V3.Y = rect.Y2;
        }

        public RectF GetRegionRect(Texture2D texture)
        {
            return new RectF(V0.Tx * texture.Width, V0.Ty * texture.Height, V2.X * texture.Width, V2.Y * texture.Height);
        }
    }
}