﻿using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Xunit;
using Moq;
using FakebookNotifications.WebApi.Hubs;
using FakebookNotifications.WebApi.HubInterfaces;
using System.Threading.Tasks;

namespace FakebookNotifications.Testing

{
    public class NotificationHubTest
    {   
        // Variables used throughout test, mocked to the appropriate interface
        private NotificationHub hub;
        private Mock<IHubCallerClients> mockClients = new Mock<IHubCallerClients>();
        private Mock<IGroupManager> mockGroups = new Mock<IGroupManager>();
        private Mock<IClientProxy> mockClientProxy = new Mock<IClientProxy>();
        private Mock<HubCallerContext> mockContext = new Mock<HubCallerContext>();
        private List<string> groupIds = new List<string>()
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
        };
        private List<string> clientIds = new List<string>() { "0", "1", "2" };


        public NotificationHubTest()
        {
            //setup for tests
            mockClients.Setup(client => client.All).Returns(mockClientProxy.Object);
            mockClients.Setup(client => client.OthersInGroup(It.IsIn<string>(groupIds))).Returns(mockClientProxy.Object);
            mockGroups.Setup(group => group.AddToGroupAsync(It.IsIn<string>(clientIds), It.IsIn<string>(groupIds), new System.Threading.CancellationToken())).Returns(Task.FromResult(true));
            mockGroups.Setup(group => group.RemoveFromGroupAsync(It.IsIn<string>(clientIds), It.IsIn<string>(groupIds), new System.Threading.CancellationToken())).Returns(Task.FromResult(true));
            mockContext.Setup(context => context.ConnectionId).Returns(It.IsIn<string>(clientIds));

            // creates hub for testing
            hub = new NotificationHub()
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = mockContext.Object
            };
        }


        /// <summary>
        /// Tests notfication hub method to send global notifications
        /// </summary>
        [Fact]
        async public void SendAllVerify()
        {
            //arranged in constructor


            // act
            await hub.SendAll("user", "test");

            // assert
            // checks to see if a message was sent to all clients, once
            mockClients.Verify(c => c.All, Times.Once);

        }

        /// <summary>
        /// Tests notfication hub method to send notifications to all users in a group
        /// </summary>
        [Fact]
        async public void SendGroupVerify()
        {
            //arrange
            string groupId = groupIds[0];
            var others = groupIds[1];

            // act
            await hub.SendGroup(groupId, "test");

            // assert
            // checks to see if a message was sent to all clients within a group, once, and not other groups
            mockClients.Verify(c => c.Group(groupId), Times.Once);
            mockClients.Verify(c => c.Group(others), Times.Never);

        }

        /// <summary>
        /// Tests notification hub method to send notification back to the user who called the method
        /// </summary>
        [Fact]
        async public void SendCallerVerify()
        {
            // arrange
            string caller = mockContext.Object.ConnectionId;


            // act
            await hub.SendCaller("caller", "test");

            // assert
            // checks to see if a message was sent to the caller-user, once, and not other users
            mockClients.Verify(c => c.Caller, Times.Once);
            mockClients.Verify(o => o.AllExcept(caller), Times.Never);
        }

        /// <summary>
        /// Tests notifcation hub method to send a notification to a specific user
        /// </summary>
        [Fact]
        async public void SendUserVerify()
        {
            // arrange

            // act
            await hub.SendUser("user", "test");

            // assert
            // checks to see if a message was sent to one specific user, once, and not other users
            mockClients.Verify(c => c.User("user"), Times.Once);
            mockClients.Verify(c => c.AllExcept("user"), Times.Never);
        }
    }
}