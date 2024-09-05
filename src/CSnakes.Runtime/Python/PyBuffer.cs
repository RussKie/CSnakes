﻿using CSnakes.Runtime.CPython;
using System.Runtime.InteropServices.Marshalling;

namespace CSnakes.Runtime.Python;
internal sealed class PyBuffer : IPyBuffer, IDisposable
{
    private readonly CPythonAPI.Py_buffer _buffer;
    private bool _disposed;
    private bool _isScalar;
    private string _format;

    /// <summary>
    /// Struct byte order and offset type see https://docs.python.org/3/library/struct.html#byte-order-size-and-alignment
    /// </summary>
    private enum ByteOrder
    {
        Native  = '@', // default, native byte-order, size and alignment
        Standard = '=', // native byte-order, standard size and no alignment
        Little = '<', // little-endian, standard size and no alignment
        Big = '>', // big-endian, standard size and no alignment
        Network = '!' // big-endian, standard size and no alignment
    }

    private enum Format
    {
        Padding = 'x',
        Char = 'b', // C char
        UChar = 'B', // C unsigned char
        Bool = '?', // C _Bool
        Short = 'h', // C short
        UShort = 'H', // C unsigned short
        Int = 'i', // C int
        UInt = 'I', // C unsigned int
        Long = 'l', // C long
        ULong = 'L', // C unsigned long
        LongLong = 'q', // C long long
        ULongLong = 'Q', // C unsigned long long
        Float = 'f', // C float
        Double = 'd', // C double
        SizeT = 'n', // C size_t
        SSizeT = 'N', // C ssize_t
    }

    public unsafe PyBuffer(PyObject exporter)
    {
        _buffer = CPythonAPI.GetBuffer(exporter);
        _disposed = false;
        _isScalar = _buffer.ndim == 0 || _buffer.ndim == 1;
        _format = Utf8StringMarshaller.ConvertToManaged(_buffer.format) ?? string.Empty;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CPythonAPI.ReleaseBuffer(_buffer);
            _disposed = true;
        }
    }

    public Int64 Length => _buffer.len;

    public bool Scalar => _isScalar;

    public bool IsReadOnly => _buffer.@readonly == 1;

    public int Dimensions => _buffer.ndim;

    private ByteOrder GetByteOrder()
    {
        // The first character of the format string is the byte order
        // If the format string is empty, the byte order is native
        // If the first character is not a byte order, the byte order is native
        if (_format.Length == 0)
        {
            return ByteOrder.Native;
        }
        return Enum.TryParse(_format[0].ToString(), out ByteOrder byteOrder) ? byteOrder : ByteOrder.Native;
    }

    private void EnsureFormat(char format)
    {
        if (!_format.Contains(format))
        {
            throw new InvalidOperationException($"Buffer is not a {format}, it is {_format}");
        }
    }

    private void EnsureScalar()
    {
        if (!Scalar)
        {
            throw new InvalidOperationException("Buffer is not a scalar");
        }
    }

    public unsafe Span<Int32> AsInt32Scalar()
    {
        EnsureScalar();
        EnsureFormat('l'); // TODO: i is also valid
        return new Span<Int32>((void*)_buffer.buf, (int)(Length / sizeof(Int32)));
    }

    public unsafe Span<UInt32> AsUInt32Scalar()
    {
        EnsureScalar();
        EnsureFormat('L'); // TODO: I is also valid
        return new Span<UInt32>((void*)_buffer.buf, (int)(Length / sizeof(UInt32)));
    }

    public unsafe Span<Int64> AsInt64Scalar()
    {
        EnsureScalar();
        EnsureFormat('q');
        return new Span<Int64>((void*)_buffer.buf, (int)(Length / sizeof(Int64)));
    }

    public unsafe Span<UInt64> AsUInt64Scalar()
    {
        EnsureScalar();
        EnsureFormat('Q');
        return new Span<UInt64>((void*)_buffer.buf, (int)(Length / sizeof(UInt64)));
    }

    public unsafe Span<float> AsFloatScalar() {
        EnsureScalar();
        EnsureFormat('f');
        return new Span<float>((void*)_buffer.buf, (int)(Length / sizeof(float)));
    }

    public unsafe Span<double> AsDoubleScalar()
    {
        EnsureScalar();
        EnsureFormat('d');
        return new Span<double>((void*)_buffer.buf, (int)(Length / sizeof(double)));
    }
}
