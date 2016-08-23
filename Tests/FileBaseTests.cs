﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace InvertedTomato.IO.Feather.Tests {
    [TestClass]
    public class FileBaseTests {
        [TestInitialize]
        public void Initialize() {
            try {
                File.Delete("test.dat");
            } catch { }
        }

        [TestMethod]
        public void Read() {
            File.WriteAllBytes("test.dat", new byte[] { 0x01, 0x00, 0x01, 0x02, 0x00, 0x02, 0x05, 0x01, 0x00, 0x03 });

            using (var file = FeatherFile.OpenRead("test.dat")) {
                PayloadReader payload;

                payload = file.Read();
                Assert.AreEqual(1, payload.OpCode);
                Assert.AreEqual(1, payload.Length);

                payload = file.Read();
                Assert.AreEqual(2, payload.OpCode);
                Assert.AreEqual(2, payload.Length);
                Assert.AreEqual(5, payload.ReadUInt8());

                payload = file.Read();
                Assert.AreEqual(3, payload.OpCode);
                Assert.AreEqual(1, payload.Length);

                payload = file.Read();
                Assert.IsNull(payload);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(EndOfStreamException))]
        public void Read_Corrupt() {
            File.WriteAllBytes("test.dat", new byte[] { 0x02, 0x00, 0x01 }); // Length says it's a 2-byte payload, yet there's only 1 byte

            using (var file = FeatherFile.OpenRead("test.dat")) {
                PayloadReader payload;

                payload = file.Read();
            }
        }

        [TestMethod]
        public void Rewind() {
            File.WriteAllBytes("test.dat", new byte[] { 0x01, 0x00, 0x01, 0x02, 0x00, 0x02, 0x05, 0x01, 0x00, 0x03 });

            using (var file = FeatherFile.OpenRead("test.dat")) {
                PayloadReader payload;
                for (var i = 0; i < 3; i++) {
                    payload = file.Read();
                    Assert.IsNotNull(payload);
                    Assert.AreEqual(0x01, payload.OpCode);
                    file.Rewind();
                }
            }
        }

        [TestMethod]
        public void Write() {
            using (var file = FeatherFile.OpenWrite("test.dat")) {
                file.Write(new PayloadWriter(0x01));
                file.Write(new PayloadWriter(0x02).Append((byte)0x05));
                file.Write(new PayloadWriter(0x03));
            }

            var bytes = BitConverter.ToString(File.ReadAllBytes("test.dat"));

            Assert.AreEqual("01-00-01" + "-02-00-02-05" + "-01-00-03", bytes);
        }

        [TestMethod]
        public void WriteAppend() {
            using (var file = FeatherFile.OpenWrite("test.dat")) {
                file.Write(new PayloadWriter(0x01));
            }
            using (var file = FeatherFile.OpenWrite("test.dat")) {
                file.Write(new PayloadWriter(0x02).Append((byte)0x05));
                file.Write(new PayloadWriter(0x03));
            }

            var bytes = BitConverter.ToString(File.ReadAllBytes("test.dat"));

            Assert.AreEqual("01-00-01" + "-02-00-02-05" + "-01-00-03", bytes);
        }
    }
}
