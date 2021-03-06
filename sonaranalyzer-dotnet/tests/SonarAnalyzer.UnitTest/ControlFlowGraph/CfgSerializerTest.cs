﻿/*
* SonarAnalyzer for .NET
* Copyright (C) 2015-2020 SonarSource SA
* mailto: contact AT sonarsource DOT com
*
* This program is free software; you can redistribute it and/or
* modify it under the terms of the GNU Lesser General Public
* License as published by the Free Software Foundation; either
* version 3 of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
* Lesser General Public License for more details.
*
* You should have received a copy of the GNU Lesser General Public License
* along with this program; if not, write to the Free Software Foundation,
* Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.ControlFlowGraph;
using SonarAnalyzer.ControlFlowGraph.CSharp;
using SonarAnalyzer.UnitTest.Helpers;

namespace SonarAnalyzer.UnitTest.ControlFlowGraph
{
    [TestClass]
    public class CfgSerializerTest
    {
        [TestMethod]
        public void Serialize_Empty_Method()
        {
            var code = @"
class C
{
    void Foo()
    {
    }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Branch_Jump()
        {
            var code = @"
class C
{
    void Foo(int a)
    {
        switch (a)
        {
            case 1:
                c1();
                break;

            case 2:
                c2();
                break;
        }
    }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{BRANCH:SwitchStatement|a}""]
0 -> 1
1 [shape=record label=""{BINARY:CaseSwitchLabel|a}""]
1 -> 2 [label=""True""]
1 -> 3 [label=""False""]
2 [shape=record label=""{JUMP:BreakStatement|c1|c1()}""]
2 -> 4
3 [shape=record label=""{BINARY:CaseSwitchLabel|a}""]
3 -> 5 [label=""True""]
3 -> 4 [label=""False""]
5 [shape=record label=""{JUMP:BreakStatement|c2|c2()}""]
5 -> 4
4 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_BinaryBranch_Simple()
        {
            var code = @"
class C
{
    void Foo()
    {
        if (true)
        {
            Bar();
        }
    }
    void Bar() { }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{BINARY:TrueLiteralExpression|true}""]
0 -> 1 [label=""True""]
0 -> 2 [label=""False""]
1 [shape=record label=""{SIMPLE|Bar|Bar()}""]
1 -> 2
2 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Foreach_Binary_Simple()
        {
            var code = @"
class C
{
    void Foo()
    {
        foreach (var i in items)
        {
            Bar();
        }
    }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{FOREACH:ForEachStatement|items}""]
0 -> 1
1 [shape=record label=""{BINARY:ForEachStatement}""]
1 -> 2 [label=""True""]
1 -> 3 [label=""False""]
2 [shape=record label=""{SIMPLE|Bar|Bar()}""]
2 -> 1
3 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Foreach_Binary_VarDeclaration()
        {
            var code = @"
namespace Namespace
    {
        public class Test
        {
            public void ForEach((string key, string value)[] values)
            {
                foreach (var (key, value) in values)
                {
                    string i = key;
                    string j = value;
                }
            }
        }
    };";

            var dot = CfgSerializer.Serialize("ForEach", GetCfgForMethod(code, "ForEach"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""ForEach"" {
0 [shape=record label=""{FOREACH:ForEachVariableStatement|values}""]
0 -> 1
1 [shape=record label=""{BINARY:ForEachVariableStatement}""]
1 -> 2 [label=""True""]
1 -> 3 [label=""False""]
2 [shape=record label=""{SIMPLE|key|i = key|value|j = value}""]
2 -> 1
3 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_For_Binary_Simple()
        {
            var code = @"
class C
{
    void Foo()
    {
        for (var i = 0; i < 10; i++)
        {
            Bar();
        }
    }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{FOR:ForStatement|0|i = 0}""]
0 -> 1
1 [shape=record label=""{BINARY:ForStatement|i|10|i \< 10}""]
1 -> 2 [label=""True""]
1 -> 3 [label=""False""]
2 [shape=record label=""{SIMPLE|Bar|Bar()}""]
2 -> 4
4 [shape=record label=""{SIMPLE|i|i++}""]
4 -> 1
3 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Jump_Using()
        {
            var code = @"
class C
{
    void Foo()
    {
        using (x)
        {
            Bar();
        }
    }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{JUMP:UsingStatement|x}""]
0 -> 1
1 [shape=record label=""{USING:UsingStatement|Bar|Bar()}""]
1 -> 2
2 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Lock_Simple()
        {
            var code = @"
class C
{
    void Foo()
    {
        lock (x)
        {
            Bar();
        }
    }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{LOCK:LockStatement|x}""]
0 -> 1
1 [shape=record label=""{SIMPLE|Bar|Bar()}""]
1 -> 2
2 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Lambda()
        {
            var code = @"
class C
{
    void Foo()
    {
        Bar(x =>
        {
            return 1 + 1;
        });
    }
}
";
            var dot = CfgSerializer.Serialize("Foo", GetCfgForMethod(code, "Foo"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Foo"" {
0 [shape=record label=""{SIMPLE|Bar|x =\>\n        \{\n            return 1 + 1;\n        \}|Bar(x =\>\n        \{\n            return 1 + 1;\n        \})}""]
0 -> 1
1 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Range()
        {
            var code = @"
internal class Test
{
    public void Range()
    {
        Range r = 1..4;
    }
}
";
            var dot = CfgSerializer.Serialize("Range", GetCfgForMethod(code, "Range"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Range"" {
0 [shape=record label=""{SIMPLE|1..4|r = 1..4}""]
0 -> 1
1 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_Index()
        {
            var code = @"
internal class Test
{
    public void Index()
    {
        Index index = ^1;
    }
}
";
            var dot = CfgSerializer.Serialize("Index", GetCfgForMethod(code, "Index"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Index"" {
0 [shape=record label=""{SIMPLE|^1|index = ^1}""]
0 -> 1
1 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_IndexInRange()
        {
            var code = @"
internal class Test
{
    public void Range()
    {
        Range range = ^2..^0;
    }
}
";
            var dot = CfgSerializer.Serialize("Range", GetCfgForMethod(code, "Range"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Range"" {
0 [shape=record label=""{SIMPLE|^2..^0|range = ^2..^0}""]
0 -> 1
1 [shape=record label=""{EXIT}""]
}
");
        }

        [TestMethod]
        public void Serialize_RangeInIndexer()
        {
            var code = @"
internal class Test
{
    public void Range()
    {
        var ints = new[] { 1, 2 };
        var lastTwo = ints[^2..^1];
    }
}
";
            var dot = CfgSerializer.Serialize("Range", GetCfgForMethod(code, "Range"));

            dot.Should().BeIgnoringLineEndings(@"digraph ""Range"" {
0 [shape=record label=""{SIMPLE|new[] \{ 1, 2 \}|1|2|\{ 1, 2 \}|ints = new[] \{ 1, 2 \}|ints|^2..^1|ints[^2..^1]|lastTwo = ints[^2..^1]}""]
0 -> 1
1 [shape=record label=""{EXIT}""]
}
");
        }

        protected IControlFlowGraph GetCfgForMethod(string code, string methodName)
        {
            (var method, var semanticModel) = TestHelper.Compile(code).GetMethod(methodName);

            return CSharpControlFlowGraph.Create(method.Body, semanticModel);
        }
    }
}
