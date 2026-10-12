namespace cpkmakecCs;

public class ArgsCmdInfo
{
	public string Command;

	public string ParamString;

	public int? ParamValue = null;

	public bool Useful;

	public bool IsMatchParam(string param)
	{
		if (param.ToUpper() == ParamString.ToUpper())
		{
			return true;
		}
		return false;
	}

	public bool IsMatchParams(string param1, string param2)
	{
		if (IsMatchParam(param1) || IsMatchParam(param2))
		{
			return true;
		}
		return false;
	}
}
