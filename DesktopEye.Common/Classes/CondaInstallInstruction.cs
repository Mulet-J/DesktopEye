using System.Collections.Generic;

namespace DesktopEye.Common.Classes;

public class CondaInstallInstruction
{
    public string Channel;
    public List<string> Packages;

    public CondaInstallInstruction(string channel, List<string> packages)
    {
        Channel = channel;
        Packages = packages;
    }
}