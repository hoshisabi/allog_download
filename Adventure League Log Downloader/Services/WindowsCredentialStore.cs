using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Adventure_League_Log_Downloader.Services;

/// <summary>
/// One generic credential in Windows Credential Manager, read and written through advapi32. The target name and the
/// UTF-16 password blob match what the <c>CredentialManagement</c> package wrote, so logins saved by earlier versions load.
/// </summary>
public sealed class WindowsCredentialStore : ICredentialStore
{
    public const string DefaultTarget = "AllogDownloader:AdventurersLeague";

    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    private readonly string _target;

    public WindowsCredentialStore(string target = DefaultTarget)
    {
        _target = target;
    }

    public bool IsAvailable => OperatingSystem.IsWindows();

    public void Save(string username, string password)
    {
        var blob = Encoding.Unicode.GetBytes(password);
        var blobPtr = Marshal.AllocHGlobal(Math.Max(blob.Length, 1));
        try
        {
            Marshal.Copy(blob, 0, blobPtr, blob.Length);
            var credential = new NativeCredential
            {
                Type = CredTypeGeneric,
                TargetName = _target,
                UserName = username,
                CredentialBlobSize = blob.Length,
                CredentialBlob = blobPtr,
                Persist = CredPersistLocalMachine
            };
            if (!CredWrite(ref credential, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }
    }

    public (string Username, string Password)? Load()
    {
        if (!CredRead(_target, CredTypeGeneric, 0, out var credentialPtr))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound)
                return null;
            throw new Win32Exception(error);
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPtr);
            var password = credential.CredentialBlobSize > 0 && credential.CredentialBlob != IntPtr.Zero
                ? Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2)
                : string.Empty;
            return (credential.UserName ?? string.Empty, password);
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public void Delete()
    {
        if (CredDelete(_target, CredTypeGeneric, 0))
            return;

        var error = Marshal.GetLastWin32Error();
        if (error != ErrorNotFound)
            throw new Win32Exception(error);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref NativeCredential credential, int flags);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, int type, int flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);
}
