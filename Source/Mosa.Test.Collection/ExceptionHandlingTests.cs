﻿/*
 * (c) 2014 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Phil Garcia (tgiphil) <phil@thinkedge.com>
 */

using System;

namespace Mosa.Test.Collection
{
	public static class ExceptionHandlingTests
	{
		public static int TryFinally1()
		{
			int a = 10;
			try
			{
				a = a + 1;
			}
			finally
			{
				a = a + 3;
			}

			a = a + 7;

			return a;
		}

		public static int TryFinally2()
		{
			int a = 10;
			int b = 13;
			try
			{
				a = a + 1;
			}
			finally
			{
				b = b + a;
			}

			b = b + 3;
			a = a + 3;

			int c = b + a;

			return c;
		}

		public static int TryFinally3()
		{
			int a = 10;
			try
			{
				try
				{
					a = a + 1;
				}
				finally
				{
					a = a + 100;
				}
			}
			finally
			{
				a = a + 3;
			}

			a = a + 7;

			return a;
		}

		public static int TryFinally4()
		{
			int a = 10;

			try
			{
				try
				{
					a = a + 1;
				}
				finally
				{
					a = a + 100;
				}
			}
			finally
			{
				a = a + 3;
			}

			a = a + 7;

			try
			{
				try
				{
					a = a + 1;
				}
				finally
				{
					a = a + 100;
				}
			}
			finally
			{
				a = a + 3;
			}

			a = a + 7;

			return a;
		}

		public static int TryFinally5()
		{
			int a = 10;

			try
			{
				a = a + 20;
			}
			catch
			{
				a = a + 30;
			}
			finally
			{
				a = a + 40;
			}

			a = a + 50;

			return a;
		}

		public static int TryFinally6()
		{
			int a = 10;

			try
			{
				a = a + 15;
				try
				{
					a = a + 20;
				}
				catch
				{
					try
					{
						a = a + 30;
					}
					catch
					{
						a = a + 40;
					}
					a = a + 50;
				}
				a = a + 55;
			}
			catch
			{
				a = a + 40;
			}

			a = a + 60;

			return a;
		}

		public static int ExceptionTest1()
		{
			int a = 10;

			try
			{
				a = a + 2;

				if (a > 0)
					throw new Exception();

				a = a + 1000;
			}
			catch
			{
				a = a + 50;
			}

			a = a + 7;

			return a;
		}

		public static int ExceptionTest2()
		{
			int a = 10;

			try
			{
				a = a + 2;

				if (a > 0)
					throw new Exception();

				a = a + 1000;
			}
			catch
			{
				a = a + 50;
			}
			finally
			{
				a = a + 2000;
			}

			a = a + 7;

			return a;
		}

		public static int ExceptionTest3()
		{
			int a = 10;

			try
			{
				try
				{
					a = a + 2;

					if (a > 0)
						throw new Exception();

					a = a + 1000;
				}
				catch
				{
					a = a + 50;
				}
				finally
				{
					a = a + 2000;
					throw new Exception();
				}
			}
			catch
			{
				a = a + 51;
			}
			finally
			{
				a = a + 5000;
			}

			a = a + 7;

			return a;
		}
	}
}