using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using PL.Logger;

namespace PL.Common.Socket.Tests
{
	[TestFixture]
	public class SenderTests
	{
		[Test]
		public void Sender_Start_TriesToConnectAfterXTime()
		{
			var stubLogFile = Substitute.For<ILogFile>();

			var senderArgs = new SenderArgs()
			{
				RetryInterval = 10,
			};

			// Arrange
			var unitUnderTest = new Sender(stubLogFile, senderArgs);
		}
	}
}
