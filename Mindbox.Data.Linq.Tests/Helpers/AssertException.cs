using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mindbox.Data.Linq.Tests
{
	public static class AssertException
	{
		public static void Throws<TException>(Action action)
			where TException : Exception
		{
			Throws<TException>(action, exception => true);
		}

		public static void Throws<TException>(Action action, Action<TException> assertExceptionAction)
			where TException : Exception
		{
			Throws<TException>(
				action,
				exception =>
				{
					assertExceptionAction(exception);
					return true;
				});
		}

		public static void Throws<TException>(Action action, string expectedMessage)
			where TException : Exception
		{
			Throws<TException>(action, exception =>
			{
				Assert.AreEqual(
					expectedMessage,
					exception.Message,
					"Возникло исключение ожидаемого типа с неожиданным сообщением.");
				return true;
			});
		}

		public static void Throws<TException>(Action action, Func<TException, bool> isCorrectExceptionPredicate)
			where TException : Exception
		{
			try
			{
				action();
			}
			catch (Exception exception)
			{
				CheckException(exception, isCorrectExceptionPredicate);
				return;
			}

			{
				var message = string.Format("Ожидалось исключение типа {0}, но оно не возникло.", typeof(TException));
				throw new AssertFailedException(message);
			}
		}

		public static async Task ThrowsAsync<TException>(Func<Task> action, Func<TException, bool> isCorrectExceptionPredicate)
			where TException : Exception
		{
			try
			{
				await action();
			}
			catch (Exception exception)
			{
				CheckException(exception, isCorrectExceptionPredicate);
				return;
			}

			{
				var message = string.Format("Ожидалось исключение типа {0}, но оно не возникло.", typeof(TException));
				throw new AssertFailedException(message);
			}
		}

		private static void CheckException<TException>(Exception exception, Func<TException, bool> isCorrectExceptionPredicate)
			where TException : Exception
		{
			var expectedException = exception as TException;
			if (expectedException == null)
			{
				var message = string.Format(
					"Ожидалось исключение типа {0}, однако возникло исключение типа {1}.\r\n\r\n{2}",
					typeof(TException),
					exception.GetType(),
					exception);
				throw new AssertFailedException(message, exception);
			}

			if (isCorrectExceptionPredicate != null && !isCorrectExceptionPredicate(expectedException))
				throw new AssertFailedException(
					string.Format(
						"Возникло исключение ожидаемого типа, но с некорректным содержимым. Сообщение: {0}",
						exception.Message),
					exception);
		}
	}
}