using System;

namespace InvertedTomato.IO.Feather {
    public interface IPayload : IDisposable {
        byte OpCode { get; }

        byte[] ToByteArray();
    }
}
