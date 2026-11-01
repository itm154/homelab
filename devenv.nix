{
  pkgs,
  lib,
  config,
  inputs,
  ...
}:

{
  dotenv.enable = true;
  packages = with pkgs; [
    yaml-language-server
    docker-language-server
    yamlfmt
  ];
}
