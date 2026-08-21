# Design Practices Shell-And-Tube Implementation Map

This document tracks the Design Practices (DP09A-F) shell-and-tube design engine as a separate calculation standard from the existing Kern engine.

## Boundary Decision

- Kern remains the current calculation standard and must not be numerically changed by Design Practices work.
- Design Practices is a separate shell-and-tube calculation standard selected from the UI.
- Both standards share the same editable `ShellAndTubeDesignVariables`, recalculation flow, SVG, checks, and datasheet surface.
- DP formulas, figures, tables, defaults, warnings, and constraints must be traceable to DP09 sections.

## Implementation Categories

- `Calculation`: numeric result used by the DP engine.
- `Constraint`: hard validity check.
- `Review`: engineering warning shown to the user.
- `Default`: initial value used only when the user has not defined a variable.
- `Datasheet`: field that should appear in the technical preview/report.
- `Figure`: chart/table that needs an explicit equation, interpolation table, or piecewise fit.

## Current Code Entry Points

- UI selector: `ShellAndTubeCalculationStandard.Kern` / `DesignPractices`.
- Dispatch: `ShellAndTubeDesignFactory`.
- DP factory: `DesignPracticesShellAndTubeDesignFactory`.
- DP engine: `DesignPracticesShellAndTubeDesign`.
- DP09D correlations: `Dp09dShellAndTubeCorrelations`.
- DP09B U ranges: `Dp09bHeatExchangerCatalog`.
- DP09C tube/material data: `Dp09cShellAndTubeCatalog`.
- DP09F preliminary/inferred zones: `Dp09fCondensationZoneModel`.
- Result traceability: `HeatExchangerDesignResult.CalculationStandard` and `Recommendations`.

## DP09A - Exchanger Types And Applications

| DP item | Type | Shell-and-tube implementation |
| --- | --- | --- |
| Exchanger type screening | Review | For this phase, DP engine is limited to shell-and-tube. Other exchanger families are not selected automatically. |
| Enhanced heat transfer technologies | Review/UI | Implemented as selectable DP09A technology flags for integral fins, nucleate boiling tubes, turbulence promoters, online mechanical cleaning, rod baffles, helical baffles, and twisted tubes. Current DP calculations remain smooth/plain-tube preliminary unless a dedicated method is implemented. |
| Two-phase condensing/vaporizing screening | Review | Current DP engine classifies no phase change, shell/tube condensation, and shell/tube vaporization. |

## DP09B - Design Considerations For All Heat Exchangers

| DP item | Type | Status |
| --- | --- | --- |
| Typical initial U values | Default/Datasheet | Implemented as DP09B Table 1 range selection. The engine first tries named refinery-service rows from stream names, then falls back to broad shell-and-tube regimes; assumed midpoint U and min/max U are shown in the datasheet. Needs remaining Table 1 rows and a structured service selector if stream names are not enough. |
| Actual, clean, and service U separation | Calculation | Implemented for DP with clean, dirty/service, and actual U. |
| Side-specific fouling | Calculation | Implemented using shell-side and tube-side allowed fouling. |
| Fouling is not a safety factor | Review | Represented through explicit fouling resistance rather than hidden U adjustment. |
| Fouling dominance review | Calculation/Review/Datasheet | Implemented as `FoulingResistanceFraction`, the fraction of service thermal resistance contributed by allowed fouling, with DP09B review recommendations. |
| Effective temperature difference | Calculation/Datasheet | Implemented as countercurrent LMTD with DP09D temperature correction factor. One-shell-pass and multiple-shell-pass correction are calculated and the LMTD correction factor is exposed in the datasheet. DP09F uses the zone-weighted LMTD model. |
| Pressure-drop allowance and utilization | Calculation/Review/Datasheet | Implemented from DP09B Table 11 preliminary allowances: liquids use 10-25 psi midpoint, gas/vapor services use pressure-regime midpoint allowances, vacuum vapor defaults conservatively to 0.5 psi, and TEMA F shell-side service is capped to the 5-10 psi range. Tube-side and shell-side utilization are shown against those allowances, with optimization review for low utilization or exceeded allowance. |
| Cooling water outlet/film temperature | Constraint/Review/Datasheet | Bulk outlet limit implemented for fresh/brackish/salt water with editable cooling-water type. Preliminary cooling-water wall and film temperatures are now estimated from duty/area heat flux and the apparent cooling-side coefficient, with film-temperature limits exposed in the datasheet. Rigorous local wall-temperature calculation remains pending. |
| Cooling water velocity/material ranges | Constraint/Review/Datasheet | DP09D Table 4 tube-side cooling-water velocity ranges implemented for the listed material/water combinations. Missing combinations are flagged for manual review rather than inferred. |
| Condenser location/drumless condenser considerations | Review/UI/Calculation/Datasheet | Implemented as editable condenser arrangement metadata. Drumless condenser criteria now calculate DP09F 110% surface allowance, 2 in vent, preliminary outlet pot diameter, 3-5 ft pot length, and 20 ft minimum shell-bottom elevation. Detailed pump NPSH, suction-line sizing, and piping layout remain review items. |

## DP09C - Shell-And-Tube Design Considerations

| DP item | Type | Status |
| --- | --- | --- |
| Tube OD/wall/gauge defaults | Default | Implemented with DP table-style common tube sizes. Needs complete tube material/standard table. |
| Tube material thermal conductivity | Calculation/UI | Implemented through DP09C material catalog and editable UI material selector. |
| Tube layout based on shell-side fouling/cleaning | Default/Review | Implemented: square layout for high shell-side fouling, triangular otherwise. |
| Tube pitch minimum | Constraint | Implemented as recommendation/check. |
| TEMA head/shell/rear-end selection | Review/Datasheet/Required method/UI | Front-head, shell type, rear-head, tube-side/shell-side cleaning method, and tube-side/shell-side corrosion allowance are editable and reflected in the TEMA preview where applicable. DP09C Table 3 fouling/rear-head/front-head/cleaning/corrosion constraints are reviewed: high shell-side fouling flags fixed-tubesheet rear heads, high tube-side fouling flags U-tubes except cooling-water/high-pressure-jetting style service, mechanical tube cleaning flags bonnet front heads, and corrosion allowance >= 1/8 in favors A front heads. Missing cleaning method or corrosion allowance is still recorded as a required review. |
| U-tube limitations by fouling | Constraint/Review | Implemented as DP09C review flag when tube-side fouling exceeds the recommended U-tube limit. |
| Maximum tube passes | Constraint/Review | Implemented from DP09C Table 4 by shell ID, with additional U-tube and floating-head single-pass construction reviews. |
| Cross baffle orientation and spacing | Constraint/Review/UI/Required method | Preliminary baffle spacing warning implemented, including DP09D baffle pitch/bundle diameter ratio 0.25-0.80. Baffle type and baffle cut are editable; DP defaults to 25% cut single-segmental baffles and records a required dedicated method for non-single-segmental baffles. |
| Shell type selection for pressure drop/vibration | Review/UI/Datasheet/Required method | TEMA shell type is now editable and reflected in the TEMA preview. DP records a required shell-side method for non-E shells; high gas/vapor shell-side pressure drop recommends reviewing TEMA X or parallel shells. |
| Impingement protection | Review | Implemented as recommendation for condensing/vaporizing shell-side services. |
| Flow-induced vibration review | Review/Required method | Implemented as review flag and required method for shell-side condensing/vaporizing service. Needs detailed post-mechanical geometry checks. |
| Manual tube-count estimation | Calculation/Datasheet/Figure | Implemented as DP09C Table 5 safe-circle/pass-partition estimate with table-based OTL for fixed/U-tube and split-ring floating-head bundles, digitized Figure 7 OTL for pull-through floating heads, and explicit correction factors for U-bend loss plus digitized/interpolated Figure 8 shell-nozzle/impingement loss. Shell-nozzle loss responds to shell-nozzle/shell-ID ratio and is exposed in the datasheet. Needs final mechanical layout verification against actual removed tubes. |
| Specification sheet fields | Datasheet | Existing preview should be expanded with DP fields and review flags. |

## DP09D - Calculation Procedure, No Change Of Phase

| DP item | Type | Status |
| --- | --- | --- |
| Estimation method sequence | Calculation | Initial DP sequence implemented independently from Kern. |
| Overdesign and optimization review | Calculation/Review/Datasheet | Implemented as area overdesign percentage plus shell/tube pressure-drop utilization metrics, used for DP09D optimization recommendations. |
| Tube-side heat transfer | Calculation/Datasheet | Implemented with turbulent, transition, and laminar branches. Laminar uses DP09D Figure 1.5 natural-convection factor, Figure 1.6 short-tube correction, lambda, and Figure 1.7 low-Prandtl correction. Grashof now uses an estimated tube-wall temperature; final film-property beta/viscosity data remain pending. |
| Tube-side pressure drop Figures 1.8, 1.9, and 1.10 | Calculation/Figure/Datasheet | Implemented as DP09D isothermal friction from Figure 1.8 multiplied by Figure 1.9 viscosity-gradient correction and Figure 1.10 natural-convection pressure-drop correction. Figure 1.9 now estimates tube-wall temperature and bulk/wall viscosity ratio from inlet/outlet viscosity data; Figure 1.10 uses the same estimated tube-wall temperature for Grashof. |
| Shell-side Figure 1.1 normal crossflow fraction | Figure/Calculation/Datasheet | Implemented as digitized/interpolated nominal crossflow fraction with DP09D pressure-drop and heat-transfer crossflow fractions stored on the design variables. Rear-head style is editable in the UI and used directly by the correlation. |
| Figure 1.2 baffle spacing correction | Figure | Implemented as DP09D correlation service with digitized-point interpolation. Needs final point audit against rendered figure. |
| Figure 1.3 low Reynolds correction | Figure | Implemented as DP09D correlation service with digitized-point interpolation. Needs final point audit against rendered figure. |
| Figure 1.4 shell-side friction and j factor | Figure | Implemented as DP09D correlation service with digitized-point interpolation and tests. The friction-factor family now includes the Figure 1.4 pitch-ratio curves for PR 1.25 and 1.33 and the DP engine uses the actual tube pitch/OD ratio. Remaining work is a finer point-by-point digitization audit for the full Reynolds range and additional pitch ratios if future DP figures/tables require them. |
| Figure 1.5 natural convection factor | Figure/Calculation/Datasheet | Implemented as a DP09D correlation service with digitized/interpolated horizontal and vertical L/D curves and used by the tube-side laminar branch. Grashof uses estimated tube-wall temperature; pending final audit against film-property data. |
| Figure 1.6 short tube correction | Figure/Calculation/Datasheet | Implemented as digitized/interpolated correlation service and used by the tube-side laminar branch. |
| Figure 1.7 low Prandtl correction | Figure/Calculation | Implemented as digitized/interpolated correction applied to tube-side heat transfer coefficient and tested. |
| Figure 2 LMTD correction factors | Figure/Calculation/Datasheet | Implemented with the standard analytic equivalent for one shell pass and the N-shell equivalent P/R transformation for multiple shell passes. The calculated F factor is stored as `LogMeanTemperatureCorrectionFactor` and reviewed when below 0.80. |
| Nozzle pressure drop limits | Calculation/Review/Datasheet | Preliminary shell/tube nozzle diameters, velocities, and pressure drops implemented with editable nozzle diameters. DP09D percentage limits are checked: shell gases/condensing vapors 35%, shell liquids 15%, tube side 40% for one pass or 35% for multiple passes. |

## DP09E - Calculation Procedure, Vaporization

| DP item | Type | Status |
| --- | --- | --- |
| Reboiler/vaporizer type selection | Review/UI | Implemented as editable `ReboilerType` with DP09E preliminary rules for kettle, internal, vertical thermosiphon, horizontal thermosiphon, and pump-through services. |
| Maximum/design heat flux | Constraint/Review/Datasheet | Preliminary heat flux is calculated and stored. DP09E Figures A3/A4 are implemented as digitized/interpolated preliminary correlations when vaporizing critical pressure and operating pressure are available. User-entered maximum allowable heat flux can override/activate design limits: 70% generally and 60% for vertical thermosiphon/choke-flow style review. Heat-flux utilization against the active design limit is now stored and exposed. |
| Vaporized fraction | Calculation/Review/Datasheet | Implemented from inlet/outlet vapor fractions for vaporizing services and used in DP09E reboiler-type checks. Preliminary vaporized-fraction limit and utilization are stored for kettle/internal and thermosiphon services. |
| Nucleate boiling coefficient | Calculation/Review/Datasheet | DP09E Figure A5 is implemented as a digitized/interpolated preliminary correlation using heat flux and vaporizing-side critical pressure. Figure A6 pressure correction is implemented from vaporizing-side reduced pressure. Figures A7/A8 are implemented as preliminary mixture-correction correlations using vaporizing-side boiling range and vapor/liquid density ratio inferred from available stream densities. Figure A9 bundle boiling correction is implemented from tube count and pitch ratio, capped at the figure's stated limit. Figure A16 natural-convection coefficient is added for vertical thermosiphon boiling. Reference coefficient, pressure correction, effective temperature range, effective minimum heat flux, BR/DR, mixture correction, single-tube coefficient, bundle correction, natural-convection correction, and bundle nucleate boiling coefficient are stored and exposed in the UI. |
| Film boiling/vapor blanketing/choke flow | Constraint/Review/Datasheet | Preliminary heat-flux limit check implemented from user-entered or Figure A3/A4-estimated maximum allowable flux, with explicit utilization against the DP design factor. Vertical thermosiphon choke-flow is now checked separately with digitized/interpolated Figure A14 reference maximum heat flux, Figure A15 tube geometry correction, and the 70% DP design factor. Figure A17 preliminary outlet vapor-fraction/mist-flow good-operation check is calculated when mass flow, density, vaporized fraction, and tube flow area are available. Integral finned-tube boiling correction is now implemented from Figures A10/A11/A12 as `Fs`, `Fe`, and `Ff`; final vendor fin geometry and outside-area basis remain a required design confirmation. |
| Thermosiphon/pump-through circulation | Calculation/Review/Datasheet/Required method | Preliminary thermosiphon vaporized-fraction and minimum heat-flux checks implemented. DP09E tower elevation rough guides are now calculated: 6-10 ft for kettle/internal, 8-20 ft for thermosiphon, and 15 ft for pump-through NPSH basis. Pump-through also exposes the approximate 10:1 circulation ratio guide. Figure A18 preliminary reboiler liquid-line diameter is estimated from the figure velocity basis when mass flow and liquid density are available. Thermosiphon static-head balance now calculates required tower elevation from vaporizing-side exchanger pressure drop, liquid density, and either a preliminary circuit allowance or explicit user-entered liquid/vapor line diameters plus K resistance coefficients. User-entered available elevation is compared against the required static head. Full piping isometrics, distributed friction by segment, distributor losses, two-phase friction, pump head, and NPSH balances remain required final methods. |
| Vaporization pressure drop | Calculation/Review/Datasheet/Required method | Implemented as explicit vaporizing-side exchanger pressure drop selected from shell/tube hydraulics and shown in datasheet. Full thermosiphon/pump-through circuit balance with inlet/outlet piping, static head, distributor losses, pump head/NPSH, and two-phase method is now marked as a required method when applicable. |

## DP09F - Calculation Procedure, Condensation

| DP item | Type | Status |
| --- | --- | --- |
| Condenser zoning | Calculation/Datasheet | Preliminary `CondensationZone` model implemented and exposed through zone count plus desuperheating/condensation/subcooling duty and area fractions. When mass flow, Cp, vapor fraction, and temperatures are available, sensible desuperheating/subcooling duties are estimated from `m Cp DeltaT` and condensation receives the remaining duty; otherwise the model falls back to vapor-fraction/temperature inference. Full T-Q curve splitting remains pending. |
| Zone-weighted LMTD | Calculation | Implemented for preliminary/inferred zone lists. Pending rigorous multiple-zone generation from T-Q data. |
| Desuperheating area | Calculation/Datasheet | Implemented as preliminary duty and area fractions. Sensible desuperheating/subcooling duty now uses available mass flow and Cp data; fallback inferred fractions remain for sparse stream data. Needs rigorous enthalpy/T-Q based duty split. |
| Hydrocarbon/steam condensing zone | Review/Required method | Implemented as explicit DP09F method boundary when composition contains water plus non-water condensing components: result requires hydrocarbon/steam dew-point and T-Q zone split, and notes that steam condensing coefficient follows the hydrocarbon condensate film coefficient in mixed zones. Detailed zone calculation remains pending. |
| Steam condensing zone | Calculation/Review | Pure steam/water condensation is now explicitly treated as pure-component condensation, so Eq. 9 vapor mass-velocity correction is suppressed and the basis is reported. Steam surface condensers remain routed to HEI as a required method. |
| Horizontal bundle condensate streams | Calculation | Initial layout-based stream count implemented. Needs full area-iteration agreement check. |
| Condensing coefficient with vapor velocity correction | Calculation/Datasheet | Horizontal-bundle condensing coefficient implemented and exposed. Vapor free-area fraction now uses the estimated vapor-volume fraction through the condensing zone instead of a simple vapor mass-fraction average, and vapor mass velocity is stored as a datasheet value. DP09F Eq. 9 vapor mass-velocity correction is applied for mixture/wide-cut condensation and suppressed for detected pure-component condensation, per DP09F note. Shell-side condensation now performs a preliminary area/coefficient iteration using the duty-weighted zone coefficient and stores iteration count plus iterated required area. Independent zone vapor/liquid properties and full Eq. 7-10 property iteration remain pending. |
| Vapor cooling coefficient | Calculation/Datasheet | Implemented as an explicit zone coefficient using the current sensible shell-side coefficient until independent zone properties are available. |
| Liquid bottom/drip cooling coefficient | Calculation/Datasheet | Implemented as explicit bottom-flow and drip-cooling coefficients for shell-side condensation. Bottom-flow uses the current sensible shell-side coefficient until independent zone properties are available; drip cooling is taken as 1.5 times the condensing coefficient and combined with bottom-flow using the DP09F preliminary 50/50 duty split. |
| Duty-weighted zone coefficient | Calculation/Datasheet | Implemented as duty-weighted effective condensing-side coefficient across desuperheating/condensation/subcooling zones. |
| Tube-side condensation coefficient | Calculation/Review | Implemented with DP09F Akers-Deans-Crosser equivalent liquid mass velocity and inside-tube coefficient equations for `TubeSideCondensation`. Wide-cut vapor cooling uses the current sensible tube-side coefficient until independent zone properties are available; liquid cooling is preliminarily assigned the condensing coefficient. |
| Condensing-side two-phase pressure drop by zone area | Calculation/Datasheet | Implemented for the current zone model. Shell-side condensation adjusts shell pressure drop; tube-side condensation adjusts tube pressure drop. The pressure drop is now summed as zone pressure-drop contributions using zone area/length fraction, preliminary zone-type pressure-drop factors, and a DP09F-style endpoint average density from the estimated vapor/liquid mass profile through desuperheating, condensation, and subcooling. Independent thermodynamic zone properties and full IX-D recalculation per zone remain pending. |
| Drumless condenser requirements | Review/UI/Calculation/Datasheet | Implemented as an explicit arrangement flag with numeric DP09F checks for 110% condensing surface, 2 in vent, outlet liquid-vapor separation pot diameter, 3-5 ft pot length, gauge-glass/anti-vortex/piping recommendations, and 20 ft minimum shell-bottom elevation. Detailed pump NPSH and suction-line velocity by selected pipe size remain pending. |
| Steam surface condenser HEI boundary | Review/UI/Required method | Implemented as an explicit surface-condenser flag with HEI-style vacuum, air-removal, and cooling-water allocation review. The DP result now marks HEI steam surface condenser sizing as a required method implementation because DP09F explicitly sends these services to HEI. Detailed HEI calculation remains pending. |

## Figure Implementation Rule

For each DP figure:

1. Use explicit DP equation when the figure gives one.
2. Use digitized points with interpolation for chart-only curves.
3. Use piecewise regression only when interpolation is not practical and the error is documented.
4. Do not silently extrapolate outside the source range.
5. Add tests for representative points and range boundaries.

## Next Implementation Steps

1. Replace preliminary laminar Grashof beta/wall-temperature assumptions with film-property values and wall-viscosity correction for Figures 1.9 and 1.10.
2. Replace DP09F inferred zone duty splits with rigorous multiple-zone generation from T-Q data.
3. Replace DP09F vapor-volume free-area estimate with zone-specific vapor/liquid property calculations and local flow-area split.
4. Add independent DP09F thermodynamic zone properties and full IX-D pressure-drop recalculation per zone beyond the current vapor/liquid endpoint density contribution model.
5. Replace preliminary DP09C U-bend tube-count allowance with mechanical layout data and validate actual removed tubes after exchanger layout is generated.
6. Replace the current K-based preliminary reboiler hydraulic checks with full segment-by-segment circuit hydraulic balances.
7. Complete finer point-by-point digitization audit for DP09D Figure 1.4 across the full Reynolds range.
