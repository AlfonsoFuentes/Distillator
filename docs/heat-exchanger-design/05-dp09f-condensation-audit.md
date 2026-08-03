# DP09F Condensation Audit

Source reviewed: `Dp09f.pdf`, Design Practices Section IX-F, "Calculation Procedure, Condensation".

This note is a working engineering map for Distillator. It avoids copying the source text and records only the calculation structure and equations we need to compare against the current shell-and-tube design code.

## Main Finding

DP09F treats condenser design as the same trial-and-error sizing problem used for liquid-liquid exchangers, but with one critical difference: condensation often needs zones because the heat-transfer mechanism and fluid properties can change along the exchanger.

The current Distillator implementation is closer to a single-zone preliminary rating. That is acceptable as a first approximation, but it is not enough to match a vendor datasheet when the service includes desuperheating, condensation, subcooling, non-condensables, or wide-cut mixtures.

## Zone Model Required

DP09F separates condenser duty into possible zones:

- Vapor cooling/desuperheating.
- Condensation of hydrocarbon or process vapor, with possible gas cooling and liquid cooling.
- Steam or water condensation, with possible subcooling.

The effective LMTD is not one global LMTD when mechanisms vary. The procedure combines zones with a weighted relationship:

```text
Q / DeltaT_effective = sum(q_zone / DeltaT_zone)
```

Each zone needs its own duty, terminal temperatures, correction factor if applicable, film properties, local coefficient, area, and pressure drop contribution.

## Area And Overall Coefficients

Preliminary area still follows the familiar first estimate:

```text
A = Q / (Uo_assumed * DeltaT_effective)
```

The vendor-style U values should be interpreted separately:

- `Actual U`: geometry-based value from the actual exchanger, `Q / (A_actual * LMTD)`.
- `Clean U`: actual/service U corrected by removing fouling resistances.
- `Service/Dirty U`: fouled design U including fouling allowance.

Enerquip's values are consistent with side fouling being applied separately:

```text
R_f,total = R_f,shell + R_f,tube
1 / U_clean ~= 1 / U_service - R_f,total
```

Distillator now has side-specific allowed fouling variables, but still needs wall/material resistance and precise clean/service naming.

## Shell-Side Condensation Coefficient

For shell-side condensation DP09F distinguishes vertical and horizontal bundles.

For horizontal bundles, the calculation needs:

- Tube count.
- Tube layout family and angle.
- Tube length available for condensation.
- Condensate mass flow in the zone.
- Number of condensate streams in the bundle, estimated by a layout-dependent power correlation.
- Film properties at zone film temperature.
- Shell free-flow area.
- Vapor and liquid average mass flow rates in the zone.
- Vapor/liquid densities and viscosities.

The horizontal-bundle path uses these calculation ideas:

```text
n_s = layout_factor * N_t^layout_exponent
Gamma = W_condensate / (L_condensing * n_s)
h_condensing_uncorrected = f(Gamma, k_f, mu_f, rho_f)
x_v = vapor fraction of shell free-flow area
G_v = W_vapor / (free_flow_area * x_v)
h_condensing = h_condensing_uncorrected * vapor_velocity_correction
h_condensing <= 2 * h_condensing_uncorrected
```

The current C++/C# mixture-condensation routine resembles part of this family, but it is simplified and does not expose the zone assumptions, area iteration, free-flow split, or liquid/gas cooling terms.

## Gas And Liquid Cooling Contributions

For wide-cut or mixed condensation, DP09F does not use only the condensing film coefficient. It also accounts for:

- Vapor/gas cooling coefficient.
- Liquid cooling coefficient.
- Drip cooling contribution for horizontal shell-side condensation.
- Weighted zone coefficient based on duty portions.

The design-side coefficient for a condensing zone is a duty-weighted combination of condensing, vapor cooling, and liquid cooling terms.

This is a key reason a vendor clean coefficient can differ from our current `1500` shortcut or simple max/sensible approach.

## Condensing-Side Pressure Drop

DP09F recommends calculating two-phase condenser pressure drop with a no-change-of-phase pressure-drop method, but using weighted average zone properties.

For each condensation zone:

```text
rho_avg_zone = 2 * W_zone / (V_vapor_in + V_liquid_in + V_vapor_out + V_liquid_out)
DeltaP_zone_base = no_change_of_phase_pressure_drop(rho_avg_zone, zone velocity, liquid viscosity)
DeltaP_zone = DeltaP_zone_base * (A_zone / A_total)
DeltaP_total = sum(DeltaP_zone)
```

Important implication: Distillator's current `CalculateCondensingShellSidePressureDrop()` is still provisional. It separated the condensing path from liquid cooling, but it uses inlet vapor density rather than true zone weighted density and does not yet weight pressure drop by zone area.

## Comparison Against Current Distillator

Current strengths:

- Template method structure now matches the old C++ calculation order.
- Tube-side water coefficient and pressure drop are close enough for the Enerquip case to be useful.
- Kern/TEMA tube-count and shell-ID selection are now in the correct design loop.
- UI can lock user geometry and recalculate.
- Side-specific allowed fouling is now represented.

Current gaps:

- No condensation zone model yet.
- Pure water vapor on shell side still uses a fixed `1500 BTU/(hr*ft2*F)` coefficient.
- Mixture condensation uses a simplified coefficient path, not the full DP09F zone weighting.
- Condensing shell-side pressure drop is separated but not yet DP09F-complete.
- Shell-side velocity shown in datasheet may be a vendor/reporting velocity definition, not necessarily the same velocity used in the DP calculation.
- Wall/material resistance is not yet included in `Clean U`.
- Effective/gross area and U-tube area counting need final naming and checks.

## Suggested Implementation Order

1. Add a small condensation-zone model for shell-and-tube design.
2. Implement zone LMTD and weighted effective LMTD.
3. Replace pure shell-side vapor condensation shortcut only after zone properties are available.
4. Implement DP09F shell-side horizontal-bundle condensation coefficient as a separate strategy.
5. Implement DP09F condensing-side pressure-drop strategy using weighted zone density and area weighting.
6. Add wall/material resistance before comparing `Clean U` against Enerquip.
7. Update the Enerquip regression test to assert shell-side velocity and pressure drop only after their reporting definition is confirmed.

## Immediate Design Decision

Do not tune constants to match Enerquip. The right next move is to add the missing concepts: zones, wall resistance, and DP09F condensing-side pressure-drop semantics. Once those exist, the Enerquip sheet becomes a strong regression case instead of a target for curve fitting.
