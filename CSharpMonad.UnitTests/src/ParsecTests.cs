﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monad;
using Monad.Parsec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpMonad.UnitTests
{
    [TestClass]
    public class ParsecTests
    {
        [TestMethod]
        public void TestBinding()
        {
            var p = from x in New.Item()
                    from _ in New.Item()
                    from y in New.Item()
                    select new ParserChar[] { x, y };

            var res = p.Parse("abcdef").Value.Single();

            Assert.IsTrue(res.Item1.First().Value == 'a' &&
                          res.Item1.Second().Value == 'c');

            Assert.IsTrue(res.Item1.First().Line == 1);
            Assert.IsTrue(res.Item1.First().Column == 1);

            Assert.IsTrue(res.Item1.Second().Line == 1);
            Assert.IsTrue(res.Item1.Second().Column == 3);

            int found = p.Parse("ab").Value.Count();

            Assert.IsTrue(found == 0);

        }

        [TestMethod]
        public void TestDigitList()
        {
            var p = from open in New.Character('[')
                    from d in New.Digit()
                    from ds in
                        New.Many(
                            from comma in New.Character(',')
                            from digit in New.Digit()
                            select digit
                            )
                    from close in New.Character(']')
                    select d.Cons(ds);

            var r = p.Parse("[1,2,3,4]").Value.Single();

            Assert.IsTrue(r.Item1.First().Value == '1');
            Assert.IsTrue(r.Item1.Skip(1).First().Value == '2');
            Assert.IsTrue(r.Item1.Skip(2).First().Value == '3');
            Assert.IsTrue(r.Item1.Skip(3).First().Value == '4');

            var r2 = p.Parse("[1,2,3,4");
            Assert.IsTrue(r2.IsFaulted);
            Assert.IsTrue(r2.Errors.First().Expected == "']'");
            Assert.IsTrue(r2.Errors.First().Input.Count() == 0);

            var r3 = p.Parse("[1,2,3,4*");
            Assert.IsTrue(r3.IsFaulted);
            Assert.IsTrue(r3.Errors.First().Expected == "']'");
            Assert.IsTrue(r3.Errors.First().Input.First().Line == 1);
            Assert.IsTrue(r3.Errors.First().Input.First().Column == 9);

        }

        [TestMethod]
        public void TestString()
        {
            var r = New.String("he").Parse("hell").Value.Single();
            Assert.IsTrue(r.Item1.AsString() == "he");
            Assert.IsTrue(r.Item2.AsString() == "ll");

            r = New.String("hello").Parse("hello, world").Value.Single();
            Assert.IsTrue(r.Item1.AsString() == "hello");
            Assert.IsTrue(r.Item2.AsString() == ", world");
        }

        [TestMethod]
        public void TestMany()
        {
            var r = New.Many(New.Character('a')).Parse("aaabcde").Value.Single();
            Assert.IsTrue(r.Item1.AsString() == "aaa");
            Assert.IsTrue(r.Item2.AsString() == "bcde");
        }

        [TestMethod]
        public void TestMany1()
        {
            var r = New.Many1(New.Character('a')).Parse("aaabcde").Value.Single();
            Assert.IsTrue(r.Item1.AsString() == "aaa");
            Assert.IsTrue(r.Item2.AsString() == "bcde");

            var r2 = New.Many1(New.Character('a')).Parse("bcde");
            Assert.IsTrue(r2.Value.Count() == 0);
        }

        [TestMethod]
        public void TestDigit()
        {
            var r = New.Digit().Parse("1").Value.Single();
            Assert.IsTrue(r.Item1.Value == '1');
        }

        [TestMethod]
        public void TestChar()
        {
            var r = New.Character('X').Parse("X").Value.Single();
            Assert.IsTrue(r.Item1.Value == 'X');
        }

        [TestMethod]
        public void TestSatisfy()
        {
            var r = New.Satisfy(c => c == 'x', "'x'").Parse("xbxcxdxe").Value.Single();
            Assert.IsTrue(r.Item1.Value == 'x');
            Assert.IsTrue(r.Item2.AsString() == "bxcxdxe");
        }

        [TestMethod]
        public void TestItem()
        {
            Assert.IsTrue(
                New.Item().Parse("").Value.Count() == 0
                );

            var r = New.Item().Parse("abc").Value.Single();
            Assert.IsTrue(
                r.Item1.Value == 'a' &&
                r.Item2.AsString() == "bc"
                );
        }

        [TestMethod]
        public void TestFailure()
        {
            var inp = "abc".ToParserChar();

            var parser = New.Failure<bool>(ParserError.Create("failed because...", inp));

            var result = parser.Parse(inp);

            Assert.IsTrue(
                result.Value.Count() == 0
                );
        }

        [TestMethod]
        public void TestReturn()
        {
            var r = New.Return(1).Parse("abc").Value.Single();
            Assert.IsTrue(
                r.Item1 == 1 &&
                r.Item2.AsString() == "abc"
                );
        }

        [TestMethod]
        public void TestChoice()
        {
            var r = New.Choice(New.Item(), New.Return(New.ParserChar('d'))).Parse("abc").Value.Single();
            Assert.IsTrue(
                r.Item1.Value == 'a' &&
                r.Item2.AsString() == "bc"
                );

            var inp = "abc".ToParserChar();

            var parser = New.Choice(
                    New.Failure<ParserChar>( ParserError.Create("failed because...",inp) ), 
                    New.Return(New.ParserChar('d'))
                )
                .Parse(inp);

            r = parser.Value.Single();

            Assert.IsTrue(
                r.Item1.Value == 'd' &&
                r.Item2.AsString() == "abc"
                );
        }

        [TestMethod]
        public void TestWhiteSpace()
        {
            var r = New.Whitespace().Parse(" ");
            Assert.IsFalse(r.IsFaulted);
            Assert.IsTrue(r.Value.Count() == 1);
            Assert.IsTrue(r.Value.Single().Item1.AsString() == " ");

        }

        [TestMethod]
        public void TestWhiteSpace2()
        {
            var r = New.Whitespace().Parse("a");
            Assert.IsFalse(r.IsFaulted);
            Assert.IsTrue(r.Value.Count() == 1);

            var empty = r.Value.Single().Item1.AsString();
            Assert.IsTrue(empty == "");
            Assert.IsTrue(r.Value.Single().Item2.AsString() == "a");
        }
    }

}
