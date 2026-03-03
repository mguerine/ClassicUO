using System;

namespace CUO_API;

public struct PluginHeader
{
	public int ClientVersion;

	public IntPtr HWND;

	public IntPtr OnRecv;

	public IntPtr OnSend;

	public IntPtr OnHotkeyPressed;

	public IntPtr OnMouse;

	public IntPtr OnPlayerPositionChanged;

	public IntPtr OnClientClosing;

	public IntPtr OnInitialize;

	public IntPtr OnConnected;

	public IntPtr OnDisconnected;

	public IntPtr OnFocusGained;

	public IntPtr OnFocusLost;

	public IntPtr GetUOFilePath;

	public IntPtr Recv;

	public IntPtr Send;

	public IntPtr GetPacketLength;

	public IntPtr GetPlayerPosition;

	public IntPtr CastSpell;

	public IntPtr GetStaticImage;

	public IntPtr Tick;

	public IntPtr RequestMove;

	public IntPtr SetTitle;
}
