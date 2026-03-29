using FluentAssertions;
using Inertia.AspNetCore;

namespace Inertia.Tests.Ssr;

public class SsrBundleDetectorTests
{
    private static SsrBundleDetector CreateDetector(
        InertiaOptions? options = null,
        Func<string, bool>? fileExists = null)
    {
        return new SsrBundleDetector(
            options ?? new InertiaOptions(),
            fileExists ?? (_ => false));
    }

    public class CustomBundle
    {
        [Fact]
        public void Detect_CustomBundle_Exists_ReturnsPath()
        {
            var detector = CreateDetector(
                new InertiaOptions { SsrBundle = "/custom/ssr.js" },
                path => path == "/custom/ssr.js");

            detector.Detect().Should().Be("/custom/ssr.js");
        }

        [Fact]
        public void Detect_CustomBundle_NotExists_ReturnsNull()
        {
            var detector = CreateDetector(
                new InertiaOptions { SsrBundle = "/custom/ssr.js" },
                _ => false);

            detector.Detect().Should().BeNull();
        }

        [Fact]
        public void Detect_CustomBundle_SkipsDefaults()
        {
            var checkedPaths = new List<string>();
            var detector = CreateDetector(
                new InertiaOptions { SsrBundle = "/custom/ssr.js" },
                path =>
                {
                    checkedPaths.Add(path);
                    return false;
                });

            detector.Detect();

            checkedPaths.Should().ContainSingle().Which.Should().Be("/custom/ssr.js");
        }
    }

    public class DefaultPaths
    {
        [Fact]
        public void Detect_CustomBundle_Null_ScansDefaults()
        {
            var checkedPaths = new List<string>();
            var detector = CreateDetector(
                new InertiaOptions { SsrBundle = null },
                path =>
                {
                    checkedPaths.Add(path);
                    return false;
                });

            detector.Detect();

            checkedPaths.Should().BeEquivalentTo(SsrBundleDetector.DefaultPaths);
        }

        [Fact]
        public void Detect_FirstDefaultExists_ReturnsFirst()
        {
            var detector = CreateDetector(
                fileExists: path => path == "wwwroot/js/ssr.js");

            detector.Detect().Should().Be("wwwroot/js/ssr.js");
        }

        [Fact]
        public void Detect_SecondDefaultExists_ReturnsSecond()
        {
            var detector = CreateDetector(
                fileExists: path => path == "wwwroot/js/ssr.mjs");

            detector.Detect().Should().Be("wwwroot/js/ssr.mjs");
        }

        [Fact]
        public void Detect_NoneExist_ReturnsNull()
        {
            var detector = CreateDetector(fileExists: _ => false);

            detector.Detect().Should().BeNull();
        }
    }
}
