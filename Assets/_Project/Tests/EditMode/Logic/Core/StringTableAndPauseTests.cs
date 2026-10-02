using Enxada.Core;
using NUnit.Framework;

namespace Enxada.Tests.Logic.Core
{
    public class StringTableAndPauseTests
    {
        private class Service { }

        [Test]
        public void Parse_ReadsKeysIgnoringCommentsAndBlankLines()
        {
            var table = StringTable.Parse("# comentário\n\nhud.time = Hora\r\n  hud.date=Data  \nsem igual\n=vazio\n");

            Assert.AreEqual(2, table.Count);
            Assert.IsTrue(table.TryGet("hud.time", out var time));
            Assert.AreEqual("Hora", time);
            Assert.IsTrue(table.TryGet("hud.date", out var date));
            Assert.AreEqual("Data", date);
        }

        [Test]
        public void Parse_KeepsEqualsSignsInsideTheValue()
        {
            var table = StringTable.Parse("a = x=y");

            table.TryGet("a", out var value);
            Assert.AreEqual("x=y", value);
        }

        [Test]
        public void Parse_UnescapesNewlinesAndBackslashes()
        {
            var table = StringTable.Parse("a = linha1\\nlinha2\\\\fim");

            table.TryGet("a", out var value);
            Assert.AreEqual("linha1\nlinha2\\fim", value);
        }

        [Test]
        public void Parse_StripsByteOrderMarkAndKeepsAccents()
        {
            var table = StringTable.Parse("﻿currency.name = Tostões");

            Assert.IsTrue(table.TryGet("currency.name", out var value));
            Assert.AreEqual("Tostões", value);
        }

        [Test]
        public void Parse_LastDuplicateWins()
        {
            var table = StringTable.Parse("a = 1\na = 2");

            table.TryGet("a", out var value);
            Assert.AreEqual("2", value);
        }

        [Test]
        public void Parse_EmptyOrNullText_GivesEmptyTable()
        {
            Assert.AreEqual(0, StringTable.Parse(null).Count);
            Assert.AreEqual(0, StringTable.Parse("").Count);
        }

        [Test]
        public void Provider_MissingKeyShowsTheKeyInBrackets()
        {
            var provider = new StringTableTextProvider(StringTable.Parse("a = 1"));

            Assert.AreEqual("[nao.existe]", provider.Get("nao.existe"));
        }

        [Test]
        public void Provider_FormatsArguments()
        {
            var provider = new StringTableTextProvider(StringTable.Parse("hud.date = {0} {1} de {2}"));

            Assert.AreEqual("Seg 1 de Primavera", provider.Format("hud.date", "Seg", 1, "Primavera"));
        }

        [Test]
        public void Provider_BadFormatTemplate_ReturnsTheRawText()
        {
            var provider = new StringTableTextProvider(StringTable.Parse("a = {0"));

            Assert.AreEqual("{0", provider.Format("a", 1));
        }

        [Test]
        public void Pause_IsPausedWhileAnyOwnerHoldsIt()
        {
            var pause = new GameplayPause();
            var dialog = new object();
            var menu = new object();

            pause.Push(dialog);
            pause.Push(menu);
            pause.Pop(dialog);
            Assert.IsTrue(pause.IsPaused);

            pause.Pop(menu);
            Assert.IsFalse(pause.IsPaused);
        }

        [Test]
        public void Pause_PushAndPopAreIdempotentAndNotifyOnlyOnChange()
        {
            var pause = new GameplayPause();
            var owner = new object();
            var changes = 0;
            pause.Changed += () => changes++;

            pause.Push(owner);
            pause.Push(owner);
            pause.Pop(owner);
            pause.Pop(owner);
            pause.Pop(new object());

            Assert.AreEqual(2, changes);
        }

        [Test]
        public void ServiceLocator_ReplaceOverwritesAndUnregisterOnlyRemovesSameInstance()
        {
            ServiceLocator.Clear();
            var first = new Service();
            var second = new Service();

            ServiceLocator.Register(first);
            ServiceLocator.Replace(second);
            Assert.AreSame(second, ServiceLocator.Get<Service>());

            Assert.IsFalse(ServiceLocator.Unregister(first));
            Assert.IsTrue(ServiceLocator.Unregister(second));
            Assert.IsFalse(ServiceLocator.TryGet<Service>(out _));
            ServiceLocator.Clear();
        }
    }
}
