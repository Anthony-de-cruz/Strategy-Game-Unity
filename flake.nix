{
  description = "Dev shell dotnet.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    { nixpkgs, flake-utils, ... }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs { inherit system; };

        dotnetSdk = pkgs.dotnetCorePackages.combinePackages (
          with pkgs.dotnetCorePackages;
          [
            sdk_10_0
            sdk_8_0
          ]
        );
      in
      {
        devShells.default = pkgs.mkShellNoCC {
          packages = [ dotnetSdk ];

          DOTNET_CLI_TELEMETRY_OPTOUT = "1";
          DOTNET_NOLOGO = "1";
          DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1";
          NUGET_XMLDOC_MODE = "skip";
        };
      }
    );
}
