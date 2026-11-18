using System;
using UnityEngine;
using System.IO;

namespace Grass
{
    public static class StreamReader
    {
        public static byte ReadByte(byte[] stream, ref int offset)
        {
            var value = stream[offset];
            offset++;
            return value;
        }

        public static byte ReadByte(MemoryStream stream)
        {
            return (byte)stream.ReadByte();
        }

        public static int ReadInt16(byte[] stream, ref int offset)
        {
            int h = ReadByte(stream, ref offset);
            int l = ReadByte(stream, ref offset);

            return (h << 8) + l;
        }

        public static int ReadInt16(MemoryStream stream)
        {
            int h = stream.ReadByte();
            int l = stream.ReadByte();
            return (h << 8) + l;
        }

        public static int ReadInt24(byte[] stream, ref int offset)
        {
            int h = ReadByte(stream, ref offset);
            int m = ReadByte(stream, ref offset);
            int l = ReadByte(stream, ref offset);

            return (h << 16) + (m << 8) + l;
        }

        public static int ReadInt24(MemoryStream stream)
        {
            int h = stream.ReadByte();
            int m = stream.ReadByte();
            int l = stream.ReadByte();

            return (h << 16) + (m << 8) + l;
        }

        public static int ReadInt32(byte[] stream, ref int offset)
        {
            int value = BitConverter.ToInt32(stream, offset);
            offset += 4;
            return value;
        }

        public static int ReadInt32(MemoryStream stream)
        {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            int value = BitConverter.ToInt32(bytes,0);
            return value;
        }

        public static float ReadFloat16(byte[] stream, ref int offset)
        {
            int h = ReadByte(stream, ref offset);
            int l = ReadByte(stream, ref offset);
            return h + l / 255f;
        }
        public static float ReadFloat16(MemoryStream stream)
        {
            int h = stream.ReadByte();
            int l = stream.ReadByte();
            return h + l / 255f;
        }

        public static float ReadFloat32(byte[] stream, ref int offset)
        {
            float value = BitConverter.ToSingle(stream, offset);
            offset += 4;
            return value;
        }
        public static float ReadFloat32(MemoryStream stream)
        {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            float value = BitConverter.ToSingle(bytes, 0);
            return value;
        }
    }

    public static class StreamWriter
    {

        public static void WriteInt16(FileStream stream, int value)
        {
            byte h = (byte)(value >> 8);
            byte l = (byte)value;
            stream.WriteByte(h);
            stream.WriteByte(l);
        }

        public static void WriteInt24(FileStream stream, int value)
        {
            byte h = (byte)(value >> 16);
            byte m = (byte)(value >> 8);
            byte l = (byte)value;
            stream.WriteByte(h);
            stream.WriteByte(m);
            stream.WriteByte(l);
        }
        public static void WriteInt32(FileStream stream, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
        public static void WriteFloat16(FileStream stream, float value)
        {
            value *= 255;
            byte h = (byte)Mathf.FloorToInt(value);
            byte l = (byte)Mathf.FloorToInt((value - h) * 255f);
            stream.WriteByte(h);
            stream.WriteByte(l);
        }

        public static void WriteFloat32(FileStream stream, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}