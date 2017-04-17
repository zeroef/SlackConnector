﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture.NUnit3;
using Should;
using SlackConnector.BotHelpers;
using SlackConnector.Connections.Sockets;
using SlackConnector.Connections.Sockets.Messages.Inbound;
using SlackConnector.Models;
using SpecsFor.ShouldExtensions;

namespace SlackConnector.Tests.Unit.SlackConnectionTests.InboundMessageTests
{
    internal class ChatMessageTests
    {
        [Test, AutoMoqData]
        public async Task should_raise_event(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                Users = { { "userABC", new SlackUser { Id = "userABC", Name = "i-have-a-name" } } },
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                User = "userABC",
                MessageType = MessageType.Message,
                Text = "amazing-text",
                RawData = "I am raw data yo"
            };

            SlackMessage receivedMessage = null;
            slackConnection.OnMessageReceived += message =>
            {
                receivedMessage = message;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            receivedMessage.ShouldLookLike(new SlackMessage
            {
                Text = "amazing-text",
                User = new SlackUser { Id = "userABC", Name = "i-have-a-name" },
                RawData = inboundMessage.RawData
            });
        }

        [Test, AutoMoqData]
        public async Task should_raise_event_given_user_information_is_missing_from_cache(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                User = "userABC",
                MessageType = MessageType.Message
            };

            SlackMessage receivedMessage = null;
            slackConnection.OnMessageReceived += message =>
            {
                receivedMessage = message;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            receivedMessage.ShouldLookLike(new SlackMessage
            {
                User = new SlackUser { Id = "userABC", Name = string.Empty }
            });
        }

        [Test, AutoMoqData]
        public async Task should_not_raise_message_event_given_incorrect_message_type(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage { MessageType = MessageType.Unknown };

            bool messageRaised = false;
            slackConnection.OnMessageReceived += message =>
            {
                messageRaised = true;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            messageRaised.ShouldBeFalse();
        }

        [Test, AutoMoqData]
        public async Task should_not_raise_message_event_given_null_message(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            bool messageRaised = false;
            slackConnection.OnMessageReceived += message =>
            {
                messageRaised = true;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, null);

            // then
            messageRaised.ShouldBeFalse();
        }

        [Test, AutoMoqData]
        public async Task should_return_expected_channel_inf0(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                SlackChatHubs = { { "channelId", new SlackChatHub { Id = "channelId", Name = "NaMe23" } } },
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                Channel = connectionInfo.SlackChatHubs.First().Key,
                MessageType = MessageType.Message,
                User = "irmBrady"
            };

            SlackMessage receivedMessage = null;
            slackConnection.OnMessageReceived += message =>
            {
                receivedMessage = message;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            SlackChatHub expected = connectionInfo.SlackChatHubs.First().Value;
            receivedMessage.ChatHub.ShouldEqual(expected);
        }

        [Test, AutoMoqData]
        public async Task should_detect_bot_is_mentioned_in_message([Frozen]Mock<IMentionDetector> mentionDetector, Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                Self = new ContactDetails { Id = "self-id", Name = "self-name" },
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                MessageType = MessageType.Message,
                Text = "please send help... :-p",
                User = "lalala"
            };

            mentionDetector
                .Setup(x => x.WasBotMentioned(connectionInfo.Self.Name, connectionInfo.Self.Id, inboundMessage.Text))
                .Returns(true);

            SlackMessage receivedMessage = null;
            slackConnection.OnMessageReceived += message => { receivedMessage = message; return Task.CompletedTask; };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            receivedMessage.MentionsBot.ShouldBeTrue();
        }

        [Test, AutoMoqData]
        public async Task should_not_raise_message_event_given_message_from_self(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                Self = new ContactDetails { Id = "self-id", Name = "self-name" },
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                MessageType = MessageType.Message,
                User = connectionInfo.Self.Id
            };

            bool messageRaised = false;
            slackConnection.OnMessageReceived += message =>
            {
                messageRaised = true;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            messageRaised.ShouldBeFalse();
        }

        [Test, AutoMoqData]
        public async Task should_not_raise_event_given_message_is_missing_user_information(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                MessageType = MessageType.Message,
                User = null
            };

            bool messageRaised = false;
            slackConnection.OnMessageReceived += message =>
            {
                messageRaised = true;
                return Task.CompletedTask;
            };

            // when
            webSocket.Raise(x => x.OnMessage += null, null, inboundMessage);

            // then
            messageRaised.ShouldBeFalse();
        }

        [Test, AutoMoqData]
        public async Task should_not_raise_exception(Mock<IWebSocketClient> webSocket, SlackConnection slackConnection)
        {
            // given
            var connectionInfo = new ConnectionInformation
            {
                WebSocket = webSocket.Object
            };
            await slackConnection.Initialise(connectionInfo);

            var inboundMessage = new ChatMessage
            {
                MessageType = MessageType.Message,
                User = "lalala"
            };

            slackConnection.OnMessageReceived += message => throw new Exception("EMPORER OF THE WORLD");

            // when & then
            Assert.DoesNotThrow(() => webSocket.Raise(x => x.OnMessage += null, null, inboundMessage));
        }
    }
}
