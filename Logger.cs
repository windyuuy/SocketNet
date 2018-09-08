
using System;
using System.Collections.Generic;
using System.Text;

namespace SocketNet{
	public class Logger{
		
		StringBuilder tags=new StringBuilder();
		int level=0;
		Dictionary<string,int> levelmap=new Dictionary<string,int>{
			["info"]=0,
			["debug"]=100,
			["warn"]=200,
			["error"]=300,
		};
		public Logger(string tag){
			tags.Append("[");
			tags.Append(tag);
			tags.Append("] ");
		}
		
		public void Log(string s){
			Info(s);
		}
		public void Info(string s){
			if(level<levelmap["info"]){return;}
			Console.WriteLine(string.Format("-{0}{2}{1}",tags,"[Info] ",s));
		}
		
		public void Debug(string s){
			if(level<levelmap["debug"]){return;}
			Console.WriteLine(string.Format("-{0}{2}{1}",tags,"[debug] ",s));
		}
		public void Warn(string s){
			if(level<levelmap["warn"]){return;}
			Console.WriteLine(string.Format("-{0}{2}{1}",tags,"[warn] ",s));
		}
		public void Error(string s){
			if(level<levelmap["error"]){return;}
			Console.WriteLine(string.Format("-{0}{2}{1}",tags,"[error] ",s));
		}
	}
}