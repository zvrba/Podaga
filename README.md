# Podaga
Collection of data structures and algorithms.  Introductory and API documentation is available [here](https://zvrba.github.io/Podaga/).

## About NuGet packages

The assemblies use strong-naming as a cheap (free) substitute for code signing certificates:  the strong name ensures that the
assembly has not been tampered with.  I can prove the ownership of the private key belonging to `PodagaSigningKey.pub.snk`.

There are no separate symbols packages because the assemblies are build with `<DebugType>embedded</DebugType>`.

## About source code

All code in `main` branch is licensed under [MPL-2.0](LICENSE).

The documentation (all content in `docs` branch) is licensed under [CC BY-NC-ND 4.0](https://creativecommons.org/licenses/by-nc-nd/4.0/) license.

`Docs` is a submodule pointing to a private repository containing documentation source; you do _not_ need to check it out.

To build the code on your own:

- Remove SBDOC project from the solution. (This is the "master" documentation source maintained in private `Docs` repositoy.)
- Disable strong-naming on projects that have it turned on. (I use strong-namig as a cheap substitute for code signing certificates.)
- Optionally, disable generation of nuget packages.

