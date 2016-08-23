﻿using System;
using System.Diagnostics;
using System.IO;

namespace InvertedTomato.IO.Feather.TestLoad {
    class Program {
        static void Main(string[] args) {
            using (var file = FeatherFile.OpenWrite("test.dat")) {
                for (var i = 1; i < 10000000; i++) {
                    file.Write(new PayloadWriter(0x00).Append(1).Append(2));
                }
            }

            using (var file = FeatherFile.OpenRead("test.dat")) {
                PayloadReader payload;
                while ((payload = file.Read()) != null) {
                    payload.ReadInt32();
                    payload.ReadInt32();
                }
            }
            
            File.Delete("test.dat");

            using (var server = FeatherTCP<TestConnection>.Listen(777)) {
                using (var client = FeatherTCP<TestConnection>.Connect("localhost", 777)) {
                    for (var i = 1; i < 100000; i++) {
                        client.TestSend();
                    }
                }
            }
        }
    }

    public class TestConnection : ConnectionBase {
        public void TestSend() {
            Send(new PayloadWriter(0x00).Append(1).Append(2));
        }
        protected override void OnMessageReceived(PayloadReader payload) {
            payload.ReadInt32();
            payload.ReadInt32();
        }
    }
}
