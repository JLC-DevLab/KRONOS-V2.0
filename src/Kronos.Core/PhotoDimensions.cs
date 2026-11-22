// Copyright 2026 JLC-DevLab. Volatility Software License 1.0.
using System.Buffers.Binary;
namespace Kronos.Core;
public sealed partial class CaseService
{
    public static (int Width,int Height) ReadPhotoDimensions(byte[] bytes)
    {
        var b=bytes.AsSpan();int width=0,height=0;
        if(b.Length>=24&&b[..8].SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
        {width=BinaryPrimitives.ReadInt32BigEndian(b[16..]);height=BinaryPrimitives.ReadInt32BigEndian(b[20..]);}
        else if(b.Length>=10&&b[..3].SequenceEqual("GIF"u8))
        {width=BinaryPrimitives.ReadUInt16LittleEndian(b[6..]);height=BinaryPrimitives.ReadUInt16LittleEndian(b[8..]);}
        else if(b.Length>=26&&b[..2].SequenceEqual("BM"u8))
        {
            if(BinaryPrimitives.ReadInt32LittleEndian(b[14..])==12){width=BinaryPrimitives.ReadUInt16LittleEndian(b[18..]);height=BinaryPrimitives.ReadUInt16LittleEndian(b[20..]);}
            else{width=BinaryPrimitives.ReadInt32LittleEndian(b[18..]);var h=BinaryPrimitives.ReadInt32LittleEndian(b[22..]);height=h==int.MinValue?0:Math.Abs(h);}
        }
        else if(b.Length>=30&&b[..4].SequenceEqual("RIFF"u8)&&b.Slice(8,4).SequenceEqual("WEBP"u8))
        {
            if(b.Slice(12,4).SequenceEqual("VP8X"u8)){width=1+b[24]+(b[25]<<8)+(b[26]<<16);height=1+b[27]+(b[28]<<8)+(b[29]<<16);}
            else if(b.Slice(12,4).SequenceEqual("VP8 "u8)&&b.Slice(23,3).SequenceEqual(new byte[]{157,1,42})){width=BinaryPrimitives.ReadUInt16LittleEndian(b[26..])&16383;height=BinaryPrimitives.ReadUInt16LittleEndian(b[28..])&16383;}
            else if(b.Slice(12,4).SequenceEqual("VP8L"u8)&&b[20]==47){width=1+b[21]+((b[22]&63)<<8);height=1+(b[22]>>6)+(b[23]<<2)+((b[24]&15)<<10);}
        }
        else if(b.Length>=4&&b[0]==255&&b[1]==216)
        {
            var position=2;
            while(position+4<=b.Length)
            {
                if(b[position++]!=255)continue;
                while(position<b.Length&&b[position]==255)position++;
                if(position>=b.Length)break;var marker=b[position++];
                if(marker==218||marker==217)break;
                if(marker==0||marker==1||marker is >=208 and <=216)continue;
                if(position+2>b.Length)break;var length=BinaryPrimitives.ReadUInt16BigEndian(b[position..]);
                if(length<2||position+length>b.Length)break;
                if(length>=7&&marker is 192 or 193 or 194 or 195 or 197 or 198 or 199 or 201 or 202 or 203 or 205 or 206 or 207)
                {height=BinaryPrimitives.ReadUInt16BigEndian(b[(position+3)..]);width=BinaryPrimitives.ReadUInt16BigEndian(b[(position+5)..]);break;}
                position+=length;
            }
        }
        if(width<=0||height<=0||width>50000||height>50000||(long)width*height>100000000)
            throw new InvalidDataException("Dimensiones no compatibles o superiores al límite de vista previa (100 MP / 50000 por lado). Se conserva el archivo.");
        return(width,height);
    }
}
