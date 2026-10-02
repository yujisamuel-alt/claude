using Enxada.Core;
using NUnit.Framework;
using UnityEngine;

namespace Enxada.Tests.Core
{
    public class EventChannelTests
    {
        [Test]
        public void VoidEventChannel_NotifiesSubscribersUntilUnsubscribed()
        {
            var channel = ScriptableObject.CreateInstance<VoidEventChannel>();
            var calls = 0;
            void Listener() => calls++;

            channel.Subscribe(Listener);
            channel.Raise();
            channel.Unsubscribe(Listener);
            channel.Raise();

            Assert.AreEqual(1, calls);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void GameConfig_DefaultsToMainMenuAfterBoot()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();

            Assert.AreEqual("MainMenu", config.FirstSceneName);
            Object.DestroyImmediate(config);
        }
    }
}
