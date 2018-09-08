
using System;
using System.Collections.Generic;
using System.Text;

namespace SocketNet{
	public class Logger{
		
		StringBuilder tags=new StringBuilder();
		int level=0;
		Dictionary<string,int> levelmap=new Dictionary<string,int>{
			["info"]=1000,
			["debug"]=1100,
			["warn"]=1200,
			["error"]=1300,
		};
		public Logger(string tag){
			tags.Append("[");
			tags.Append(tag);
			tags.Append("] ");
		}
		
		public void Log(string s){
			Info(s);
		}
		public void Info<T>(T s){
			if(level>levelmap["info"]){return;}
			Console.WriteLine(string.Format("-{0}{1}{2}",tags,"[Info] ",s));
		}
		
		public void Debug<T>(T s){
			if(level>levelmap["debug"]){return;}
			Console.WriteLine(string.Format("-{0}{1}{2}",tags,"[debug] ",s));
		}
		public void Warn<T>(T s){
			if(level>levelmap["warn"]){return;}
			Console.WriteLine(string.Format("-{0}{1}{2}",tags,"[warn] ",s));
		}
		public void Error<T>(T s){
			if(level>levelmap["error"]){return;}
			Console.WriteLine(string.Format("-{0}{1}{2}",tags,"[error] ",s));
		}
	}
}