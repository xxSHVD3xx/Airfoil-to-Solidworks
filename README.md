# Airfoil to SolidWorks

A small Windows application that converts AirfoilTools CSV exports into point files for SolidWorks **Curve Through XYZ Points**.

## Download and use

1. Download [`AirfoilToSolidWorks.exe`](AirfoilToSolidWorks.exe) from this repository (open the file and click **Download raw file**).
2. Run it on Windows. Python is not required; the application uses .NET Framework 4.x.
3. Drop an AirfoilTools CSV file onto the window or choose it with **Browse**.
4. Check the airfoil details shown from the input file. Missing details are shown as a dash. **Max thickness (mm)** is calculated from the surface coordinates: the greatest vertical separation of the two linearly interpolated surfaces at the same X coordinate. This assumes a contour ordered from trailing edge to leading edge and back, with each surface monotonic in X; otherwise the value is shown as a dash. Rotated profiles (nonzero pitch) may not represent thickness perpendicular to the chord.
5. Choose an output path if needed and click **Convert**. By default the output is saved beside the CSV as `<input>_sw.txt`.
6. In SolidWorks, use **Insert → Curve → Curve Through XYZ Points** and select the output file.

The output is a tab-separated list of X, Y, Z coordinates with `mm` units; Z is always zero. Only points in the `Airfoil surface` section are exported. The coordinates are written as provided in the source CSV, without rescaling. Existing output files require confirmation before replacement.

## Build from source

On Windows with .NET Framework 4.x and its C# compiler installed, run in PowerShell from the repository root:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /win32manifest:app.manifest /out:AirfoilToSolidWorks.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll AirfoilToSolidWorks.cs
```

The manifest enables system-DPI awareness. If you move the window between monitors with different scaling, restart it on the target monitor if needed.

## Files

- `AirfoilToSolidWorks.exe` — ready-to-run Windows application.
- `AirfoilToSolidWorks.cs` and `app.manifest` — application source and Windows manifest.
- `airfoil2sw.py` — original command-line converter; run `python airfoil2sw.py --help` for its options.

