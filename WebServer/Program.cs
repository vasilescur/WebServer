using System;
using System.Text;
using System.Threading;
using System.Net;

namespace WebServer
{
	class WebServer
	{
		private readonly HttpListener listener = new HttpListener();
		private readonly Func<HttpListenerRequest, string> responderMethod;

		public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
		{
			if (!HttpListener.IsSupported)
				throw new NotSupportedException();

			if (prefixes == null || prefixes.Length == 0)
				throw new ArgumentException("Prefixes required");

			if (method == null)
				throw new ArgumentException("Method required");

			foreach (string s in prefixes)
				listener.Prefixes.Add(s);

			responderMethod = method;
			listener.Start();
		}

		public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
			: this(prefixes, method) { }


		public void Start()
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
				Console.WriteLine("Server running...\n");

				try
				{
					while (listener.IsListening)
					{
						Console.WriteLine("Listening for a request...");
						ThreadPool.QueueUserWorkItem((c) =>
						{
							var ctx = c as HttpListenerContext;
							try
							{
								string responseStr = responderMethod(ctx.Request);
								byte[] buffer = Encoding.UTF8.GetBytes(responseStr);

								ctx.Response.ContentLength64 = buffer.Length;
								ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);

								Console.WriteLine("Request recieved:" +
									"UserHostAddress:\t{0}\n" +
									"UserAgent\t\t:{1}\n" +
									"QueryString:\t{2}\n\n", ctx.Request.UserHostAddress, ctx.Request.UserAgent, ctx.Request.QueryString);
							}
							catch { }   // Suppress exceptions
							finally
							{
								// Always close stream
								ctx.Response.OutputStream.Close();
							}
						}, listener.GetContext());
					}
				}
				catch { }	// Suppress exceptions
			});
		}

		public void Stop()
		{
			listener.Stop();
			listener.Close();
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			WebServer server = new WebServer(new string[] { "http://localhost:1234/" }, SendResponse);
			server.Start();
			Console.WriteLine("Server running... Press any key to quit.");
			Console.ReadKey();
			server.Stop();
		}

		public static string SendResponse(HttpListenerRequest request)
		{
			return string.Format(@"
				<html>
					<body>
						This is a test HTML document.
						<br/>
						{0}5
					</body>
				</html>
					", DateTime.Now);
		}
	}
}
