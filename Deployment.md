# Introduction #

You must be aware of these ASPNET concerns when deploying to an ASPNET server.


# Details #

1. You must give the ASPNET user (IISUSR or ASPNET or Network services user) permission to modify (read/write) the **/PDF** and **/render** directories.

2. The DLL **gsdll32.dll** must be available to the page. You might have to place it in \Windows\System32 or appropriate DLL path for your 32 bit operating system.

3. Currently only 32 bit OS is supported.