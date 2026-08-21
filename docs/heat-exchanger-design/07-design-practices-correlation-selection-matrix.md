# Design Practices Shell-And-Tube Correlation Selection Matrix

Purpose: define which Design Practices procedure/correlation must be selected before changing heat-transfer coefficient or pressure-drop code. This document is intentionally a review matrix first; implementation must follow after confirmation against the PDF pages/figures.

## Source Scope

- DP09D, Calculation Procedure, No Change Of Phase, December 2001.
- DP09E, Calculation Procedure, Vaporization, December 2000.
- DP09F, Calculation Procedure, Condensation, December 1999.
- DP09C, Design Considerations For Shell And Tube Exchangers, December 2001, for geometry/construction constraints only.
- DP09B, Design Considerations For All Types Of Heat Exchangers, December 2002, for initial typical overall U, fouling, and service-level design guidance.

## Implementation Rule

- Do not hide correlation selection inside large calculation methods.
- Use explicit selector/strategy classes where the selection changes by phase regime, side, fluid class, or exchanger type.
- Every implemented correlation must carry a source note with DP section/page/table/figure so debug review can go directly to the book.
- Graph-based lookups should be implemented as named chart/table services with tests against known points from the DP sample calculations.
- Regression equations for DP figures are allowed only after visual/digitized review confirms error is acceptable over the intended range.

## Matrix

| Case | Side | Trigger | Heat-transfer route | Pressure-drop route | Source to confirm | Implementation status |
|---|---:|---|---|---|---|---|
| No phase change, liquid/liquid | Tube | Both tube inlet/outlet liquid; no vaporization/condensation | DP09D estimation method. Tube-side coefficient from the DP09D tube-side procedure/figures, with turbulent-flow validity check. | DP09D tube-side pressure-drop procedure, including tube friction and return/pass effects as applicable. | DP09D pages 6-7 procedure; Table 1A/1B; Figures 1.8 and 1.9. | Pending strategy split. Current code uses generic DP helper and must be audited. |
| No phase change, vapor/liquid or vapor/vapor | Tube | No phase change, tube stream vapor or gas-like | DP09D estimation method; validate gas/vapor property basis and Reynolds range. | DP09D tube-side pressure-drop procedure with gas density/velocity basis. | DP09D pages 3-7; Table 1A/1B. | Pending. Do not reuse liquid-only assumptions. |
| No phase change, any shell-side single phase | Shell | Shell stream has no phase change | DP09D shell-side estimation method, based on HTRI stream-analysis simplification and correction factors. | DP09D shell-side pressure-drop estimation, including baffle/crossflow/window/leakage correction factors available in DP figures. | DP09D pages 3-7; Figures 1.1 through 1.7; Figure 3 for stream analysis context. | Partially approximated. Needs explicit shell-side strategy and chart tests. |
| Temperature correction, no phase change | Exchanger | Multipass shell-and-tube, non-countercurrent correction needed | Corrected LMTD using DP09D Figure 2 family based on shell arrangement and R/P. | N/A | DP09D Figures 2, 2A-2F. | Implemented approximately; must verify figure selection by TEMA arrangement. |
| Condensation, simple isothermal steam or nearly single-zone condenser | Condensing side | Vapor fraction decreases and condensing temperature is effectively flat | DP09F condenser zoning still owns LMTD. If no desuperheating/sharp break is present, one zone may be adequate. Condensing-side coefficient must follow DP09F, not Kern shortcut. | DP09F condensing-side pressure-drop estimate by zone when two-phase behavior exists; otherwise appropriate DP09D single-phase route for noncondensing side. | DP09F pages 2-4, 8, 14; Table 2. | Zoning scaffold exists. Condensing coefficient still not complete. |
| Condensation, wide-cut hydrocarbon or multi-zone | Condensing side | Heat-release curve has desuperheating, condensation, subcooling, steam/water break, or wide boiling range | DP09F zone model: split T-Q curve, calculate zone LMTDs, zone U values, and zone areas. Shell/tube condensing equations depend on side. | DP09F zone pressure drops; calculate at zone conditions and prorate by zone area, then sum. | DP09F pages 2-14; Figure 1; Table 1; Table 2; Figures 2A, 3B, 4, 5, 6A/6B as applicable. | Preliminary/inferred zones only. Full flash/T-Q zoning pending thermodynamic flash integration. |
| Condensation with noncondensing side in tubes, water/aqueous solution | Tube noncondensing side | Tube-side fluid is water or aqueous solution; condensing side elsewhere | Use DP09F water tube-side formula for `hio`, not generic turbulent coefficient. Enerquip check with current data gives about `1643 BTU/(hr*ft2*F)`. | Use DP09F water tube-side pressure-drop formula for this case. Enerquip check with current data gives about `4.04 psi` using DP09D plain-steel `Ft`. | DP09F Table 2 calculation sheet, page 24 in the user's PDF view; DP09D pressure-drop fouling factor page 41. | Implemented for water/aqueous tube side with shell-side condensation. Steel-tube `Ft` is implemented explicitly. Alloy-tube `Ft` is flagged in recommendations until the DP09D alloy table/equation is digitized cleanly. |
| Condensation with noncondensing side not water | Noncondensing side | Noncondensing fluid is process liquid/vapor, not water/aqueous | DP09F sends noncondensing side back to the appropriate manual section. For single phase, use DP09D. | DP09D pressure drop for the noncondensing side. | DP09F page 9, step 6; DP09D relevant table/figures. | Pending selector. |
| Condensing side pressure drop | Condensing side | Two-phase pressure drop in condenser zones | Use DP09F weighted average density/velocity and DP09D no-phase equations at zone conditions; zone DP is area-weighted; total is sum of zones. | Same route; independent Lockhart-Martinelli check only as optional warning due broad uncertainty. | DP09F pages 7-8 and 14. | Pending. Current implementation is not definitive. |
| Vaporization/reboiler, kettle/internal | Boiling side | Pool boiling in kettle or internal reboiler | DP09E kettle/internal reboiler heat-transfer forms; max/design heat flux and nucleate boiling charts. | DP09E kettle hydraulic balance forms, where applicable. | DP09E pages 8-12; Appendices A/B; Figures A3-A13; Appendices F/G. | Not implemented. Build after reboiler model is explicit. |
| Vaporization/reboiler, vertical thermosiphon tubeside | Tube boiling side | Vertical thermosiphon, vaporization in tubes | DP09E vertical thermosiphon heat-transfer form; check vaporization fraction limits and stability recommendations. | DP09E hydraulic balance form for natural circulation, including circuit pressure balance. | DP09E pages 5, 11; Appendices C/D and H/I; Figures A14-A18 as applicable. | Not implemented. Needs dedicated reboiler strategy. |
| Vaporization/reboiler, horizontal thermosiphon shellside | Shell boiling side | Horizontal thermosiphon, boiling on shell side | DP09E horizontal shell-side reboiler route; shell-side boiling and vapor fraction limits by service class. | DP09E hydraulic balance for horizontal shellside thermosiphon. | DP09E pages 6, 12; Appendix J and related figures. | Not implemented. |
| Pump-through reboiler | Boiling side | Forced circulation by pump | DP09E pump-through route; allows higher pressure drop and higher coefficients, with outlet vapor fraction limits for fouling service. | DP09E pump-through hydraulic route; pump/NPSH belongs to process system boundary, not generic exchanger sizing UI unless reboiler model includes circulation loop. | DP09E page 6 and Appendix E. | Not implemented. Keep separate from normal shell-and-tube exchanger design. |

## Immediate Checks For Enerquip E-101

- Service classification should identify shell side as steam condensing and tube side as aqueous solution/water-like, regardless of stream display names such as `S-102 S-103` or `CIP`.
- Tube count must be constructible with tube passes. For two tube passes, required/maximum/actual tube counts must be even.
- Effective temperature difference for the current simple steam condensation case may be a single inferred zone when inlet/outlet condensing temperature is essentially constant, but the code must label it as preliminary until full T-Q zoning is available.
- Tube-side coefficient expected by DP09F water formula with current debug values:
  - `V = 6.5865 ft/s`
  - `di = 0.555 in`
  - `do = 0.625 in`
  - `tt = 158 F`
  - `hio ~= 1643 BTU/(hr*ft2*F)`

## Next Implementation Shape

1. Create DP correlation selector objects for:
   - `Dp09dNoPhaseChangeTubeSideStrategy`
   - `Dp09dNoPhaseChangeShellSideStrategy`
   - `Dp09fWaterTubeSideNonCondensingStrategy`
   - `Dp09fCondensingSideZoneStrategy`
   - DP09E reboiler strategies later.
2. Move current large-method coefficient and pressure-drop branching behind these strategies.
3. Add unit tests for each selector decision before changing formulas.
4. Add graph/table lookup tests using DP sample-calculation points.
5. Only then replace the current generic tube-side coefficient for the DP09F water/aqueous case.
