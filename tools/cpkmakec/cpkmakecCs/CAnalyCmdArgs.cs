using System;
using System.Collections.Generic;

namespace cpkmakecCs;

public class CAnalyCmdArgs
{
	private List<ArgsCmdInfo> m_cmdlist;

	private List<string> m_fnamelist;

	public List<ArgsCmdInfo> Command => m_cmdlist;

	public List<string> Filename => m_fnamelist;

	public CAnalyCmdArgs()
	{
		m_cmdlist = new List<ArgsCmdInfo>();
		m_fnamelist = new List<string>();
	}

	~CAnalyCmdArgs()
	{
	}

	public bool AnalizeCmdArgs(string[] cmdline)
	{
		m_cmdlist.Clear();
		foreach (string text in cmdline)
		{
			ArgsCmdInfo argsCmdInfo = analizeCmdArg(text);
			if (argsCmdInfo != null)
			{
				m_cmdlist.Add(argsCmdInfo);
			}
			else
			{
				m_fnamelist.Add(text);
			}
		}
		if (m_cmdlist.Count > 0 || Filename.Count > 0)
		{
			return true;
		}
		return false;
	}

	private ArgsCmdInfo analizeCmdArg(string command)
	{
		ArgsCmdInfo argsCmdInfo = new ArgsCmdInfo();
		if (!command.StartsWith("-") && !command.StartsWith("/"))
		{
			return null;
		}
		command = command.Substring(1, command.Length - 1);
		int num = command.IndexOf('=');
		if (num > 0)
		{
			argsCmdInfo.Command = command.Substring(0, num);
			argsCmdInfo.ParamString = command.Substring(num + 1);
			argsCmdInfo.ParamValue = null;
			try
			{
				int result = 0;
				if (int.TryParse(argsCmdInfo.ParamString, out result))
				{
					argsCmdInfo.ParamValue = result;
				}
			}
			catch (Exception)
			{
				argsCmdInfo.ParamValue = null;
			}
			finally
			{
			}
		}
		else
		{
			argsCmdInfo.Command = command;
		}
		return argsCmdInfo;
	}

	public ArgsCmdInfo GetMatchArgs(string cmd)
	{
		cmd = cmd.ToUpper();
		foreach (ArgsCmdInfo item in m_cmdlist)
		{
			if (item.Command == null || !item.Command.ToUpper().Equals(cmd))
			{
				continue;
			}
			item.Useful = true;
			return item;
		}
		return null;
	}

	public ArgsCmdInfo GetInvalidCommand()
	{
		foreach (ArgsCmdInfo item in m_cmdlist)
		{
			if (!item.Useful)
			{
				return item;
			}
		}
		return null;
	}
}
