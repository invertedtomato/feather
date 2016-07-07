﻿using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace InvertedTomato.Feather.Tests {
	[TestFixture]
	public class ConnectionBaseTests {
		[Test]
		public void Send() {
			using (var connection = new FakeConnection()) {
				Assert.AreEqual("01-00-01", BitConverter.ToString(connection.TestSend(0x01, new byte[] { })));
				Assert.AreEqual("09-00-02-01-02-03-04-05-06-07-08", BitConverter.ToString(connection.TestSend(0x02, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));
			}
		}

		[Test]
		public void Receive() {
			using (var connection = new FakeConnection()) {
                Assert.AreEqual("01", BitConverter.ToString(connection.TestReceive(new byte[] { 1, 0, 1 }))); // Opcode, no params
                Assert.AreEqual("01-02", BitConverter.ToString(connection.TestReceive(new byte[] { 2, 0, 1,2 }))); // Opcode with params
				Assert.AreEqual("01-02-03-04-05-06-07-08", BitConverter.ToString(connection.TestReceive(new byte[] { 8, 0, 1, 2, 3, 4, 5, 6, 7, 8 })));
			}
		}


		[Test]
		public void KeepAliveSend() {
			using (var connection = new FakeConnection(new Options() { KeepAliveInterval = 50 })) {
				// Allow time for keep-alive - timers are finicky with such small intervals
				Thread.Sleep(400);

				// Check they were sent
				var result = connection.Socket.Stream.ReadOutput();
				Assert.IsTrue(result.All(a => a == 0), "Incorrect data sent.");
				Assert.IsTrue(result.Length >= 4, "Not enough keep-alives received - " + result.Length + ".");
				Debug.Write(result.Length / 2 + " keep alives.");
			}
		}

		[Test]
		public void KeepAliveDisconnect() {
			using (var connection = new FakeConnection(new Options() { ReceiveTimeout = 50 })) {
				// Check not disposed
				Assert.IsFalse(connection.IsDisposed);

				// Allow time for keep-alive s
				for (var i = 1; i < 10 && !connection.IsDisposed; i++) {
					Thread.Sleep(100);
				}

				// Check disposed
				Assert.IsTrue(connection.IsDisposed);
			}
		}
		[Test]
		public void BooleanGetBytes() {
			Assert.AreEqual(new byte[] { 1 }, true.GetBytes());
			Assert.AreEqual(new byte[] { 0 }, false.GetBytes());
		}
		[Test]
		public void EndToEnd() {
			var clientConnected = 0;
			var clientDisconnected = 0;
			var clientPings = 0;
			var serverPings = 0;
			var options = new Options() {
				NoDelay = true
			};

			// Create server
			using (var server = Feather<RealConnection>.Listen(1234, options)) {
				server.OnClientConnected += (connection) => {
					clientConnected++;
					connection.OnPingReceived += () => { serverPings++; };
					connection.OnDisconnected += (reason) => { clientDisconnected++; };

					// Send pings
					connection.SendPing();
					connection.SendPing();
					connection.SendPing();
				};

				// Create client
				using (var client = Feather<RealConnection>.Connect("localhost", 1234, options)) {
					client.OnPingReceived += () => { clientPings++; };

					// Send pings
					client.SendPing();
					client.SendPing();

					// Wait for sending to complete
					Thread.Sleep(50);

					// Dispose and check
					client.Dispose();
					Assert.IsTrue(client.IsDisposed);
				}

				// Wait for sending to complete
				Thread.Sleep(50);

				// Dispose and check
				server.Dispose();
				Assert.IsTrue(server.IsDisposed);
			}

			// Check all worked
			Assert.AreEqual(1, clientConnected);
			Assert.AreEqual(1, clientDisconnected);
			Assert.AreEqual(2, serverPings);
			Assert.AreEqual(3, clientPings);
		}

		// TODO:
		// Remote disconnect
		// Local disconnect
		// Connection interrupted
	}
}
