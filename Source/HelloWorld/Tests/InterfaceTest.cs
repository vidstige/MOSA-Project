﻿using Mosa.Platforms.x86;
using Mosa.Kernel;
using Mosa.Kernel.X86;
using System;

namespace Mosa.HelloWorld.Tests
{
	public class InterfaceTest : KernelTest
	{
		public static void Test()
		{
			Screen.SetCursor(23, 22);
			Screen.Write("Interface Test: ");

			PrintResult(InterfaceTest1());
			PrintResult(InterfaceTest2());
			PrintResult(InterfaceTest3());
		}

		public static bool InterfaceTest1()
		{
			TestClass tc = new TestClass();
			bool result = tc.B() == 3;
			return result;
		}

		public static bool InterfaceTest2()
		{
			TestClass tc = new TestClass();
			IInterfaceB b = tc;
			bool result =  (b.B() == 3);
			return result;
		}

		public static bool InterfaceTest3()
		{
			TestClass tc = new TestClass();
			IInterfaceB b = tc;
			bool result = (b.A() == 2);
			return result;
		}
	}

	public interface IInterfaceA
	{
		int A();
	}

	public interface IInterfaceB
	{
		int A();
		int B();
	}

	public class TestClass : IInterfaceA, IInterfaceB
	{
		public int A()
		{
			return 1;
		}

		int IInterfaceB.A()
		{
			return 2;
		}

		public int B()
		{
			return 3;
		}
		
	}
}