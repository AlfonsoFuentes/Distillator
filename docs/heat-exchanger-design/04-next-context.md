# Heat Exchanger Design - Next Context

This context captures the next work items for Distillator heat exchanger design. It exists to keep the UI, thermal design, TEMA construction checks, pressure vessel mechanics, and future equipment types separated and traceable.

## Current Design Philosophy

The shell-and-tube design workflow must combine three knowledge sources without mixing their responsibilities:

- Kern: thermal and hydraulic design. Use it for heat duty, assumed dirty overall coefficient iteration, required area, tube count, shell diameter selection, tube/shell heat transfer coefficients, pressure drops, velocities, fouling resistance, and optimization against thermal/hydraulic restrictions.
- TEMA: industrial shell-and-tube construction standard. Use it for nomenclature, TEMA type, datasheet/specification sheet layout, tube pitch construction checks, tube layout cleaning implications, baffle/support rules, impingement protection, vibration review flags, and construction-quality checks.
- Megyesy / pressure vessel design: mechanical fabrication. Use it after the thermal/TEMA geometry is selected to calculate pressure vessel details such as shell/head thickness, corrosion allowance, nozzles, reinforcement pads, supports, welding, test pressure, and fabrication sheet details.

The UI must stay visual and elegant, but the backend must remain the source of truth for calculations. User-defined edits in the UI must be stored through `Variable<T>` so recalculation respects user intent.

## 1. Finish The UI And Prove It Works

Goal: restore the shell-and-tube design tab to the polished visual experience while connecting it to the new backend design flow.

Expected behavior:

- If no design exists, the user sees only `Create Design`.
- `Create Design` creates one best initial design, not a batch of alternatives.
- Each created design appears as a tab using the same visual language as the base equipment dialog tabs.
- Selecting a design tab shows its own variables, results, SVG geometry, checks, and datasheet.
- UI edits update the underlying `Variable<T>` as user-defined input and immediately call recalculation.
- Unit selectors must update both the displayed unit and displayed value consistently.
- The SVG must respond visually to geometry changes.

Required visual sections:

- Tube detail: standard, size, gauge/thickness, OD/ID representation.
- Tube layout: shell ID, pitch, triangular/square layout, required/actual/maximum tube count.
- Exchanger view: TEMA type, tube construction, tube passes, shell passes, tube length, baffle spacing, straight/U-tube visual difference.
- Design constraints: hard pass/review/fail checks.
- Design quality: oversize, pressure drop utilization, shell fill, warnings that are not hard equations.
- TEMA-style datasheet: compact technical table, not generic UI cards.

Validation:

- Manual UI test with the current E-101 condenser case.
- Manual UI test editing shell ID, tube count, pitch, tube length, baffle spacing, tube passes, and units.
- Build `Client` after UI changes.

Additional UI fixes found during the Enerquip validation:

- Completed: improve the `Streams` tab vertical layout so the stream grid uses the available dialog height and does not leave a large empty area below the table.
- Completed: `Gauge` in Tube Detail is a dropdown, not a free numeric text box.
- Completed: when `Standard`, `Tube OD`, or `Gauge/BWG` changes, calculate `Tube ID` from the selected wall thickness. Do not allow physically impossible combinations such as `ID > OD`.
- Completed: keep `Tube ID` as a calculated value for standard tubes unless a future explicit custom/manual tube mode is added.
- Completed: improve the `Tube Layout` SVG so installed tubes are visually centered and available positions are shown in a softer style.

## 2. UI Example Case From The Provided Spec Sheet

Goal: use the provided Enerquip-style specification sheet as a concrete UI/report reference case.

Known source:

- `C:\Users\alfon\Downloads\Enerquip Spec Sheet 65168.pdf`

Expected use:

- Use the PDF as a visual pattern for how data should be grouped and printed.
- Recreate the same kind of datasheet organization in the design tab and future report.
- Create a unit/regression test where values that should be calculated by the design backend are not pre-filled manually.

Important rule:

- Do not define calculated values by hand in the test just to make it pass. Values such as heat duty, LMTD, tube flow area, tube clearance, tube surface area, actual area, velocities, pressure drops, dirty coefficient, and fouling resistance must be calculated by equipment logic.

Enerquip 65168 comparison findings:

- The geometry entered through UI can closely match the spec sheet:
  - TEMA type `B-E-U`.
  - Tube OD `0.625 in`.
  - Tube wall thickness `0.035 in`, approximately `20 BWG`.
  - Tube ID about `0.555 in`.
  - Tube length `4 ft`.
  - Pitch `0.78125 in`.
  - Shell ID `6.407 in`.
  - Baffle spacing `9.5 in`.
  - Tube passes `2`.
- Completed: `Tube No. 16U` in the vendor sheet likely means `16 U-tubes`, while the model counts `32 tube legs`. The UI now makes this distinction explicit for U-tube construction.
- Completed: add `Actual U` to the datasheet preview. It is calculated from the defined exchanger geometry using `Q = U * A * LMTD`, so `Actual U = Q / (Actual Area * LMTD)`.
- Completed: separate vendor fouling by side in the model. The design now has tube-side allowed fouling and shell-side allowed fouling, and derives the total allowed fouling from both values.
- Completed: shell-side vapor-condensing designs now use a separated shell-side pressure-drop method instead of reusing the liquid cooling pressure-drop path.
- Keep comparing `Service U`, `Clean U`, and `Actual U` carefully. `Actual U` is the geometry-based value. `Clean U` still does not include all construction details that a vendor sheet may include, such as tube wall/material resistance.
- The current fixed shell-side condensing coefficient of `1500 BTU/(hr*ft2*F)` is still a Kern-style shortcut/recommendation for pure water vapor. Enerquip may use a more detailed condensing calculation or construction corrections; validate this before replacing it.
- The current shell-side vapor/condensing velocity still differs strongly from the vendor sheet. Review the shell-side velocity definition used by TEMA/vendor datasheets before treating it as a hard mismatch.
- Tube-side velocity compares well against the vendor sheet, but tube-side pressure drop still needs review for U-tube return/passes/minor losses.
- Area checks need to account for the industrial meaning of gross/effective area and U-tube area counting before being treated as final quality judgments.

## 3. Missing Concrete Implementations Of `HeatExchangerDesign`

Goal: finish the design cases that the abstract template method expects.

Existing direction:

- `HeatExchangerDesign` is the template method.
- Concrete classes must override only the parts that differ by process/type.
- Common methods must remain common: tube inner diameter, tube clearance, tube surface area, heat duty, LMTD, assumed area, required tube count, shell flow area, equivalent diameter when common, clean overall coefficient, fouling resistance structure, and shared result construction.

Known concrete cases already started:

- Shell-side pure water vapor condensing / tube-side liquid water.
- Shell-side vapor mixture condensing / tube-side liquid water.
- Shell-side vapor condensing / tube-side liquid mixture.
- Shell-side liquid cooling / tube-side liquid water.
- Shell-side liquid cooling / tube-side liquid mixture.

Cases still requiring implementation and validation:

- Shell-side process with tube-side vaporizing liquid.
- Shell-side process with tube-side two-phase outlet.
- Shell-side process with tube-side vapor.
- Additional combinations where shell side is condensing but tube side is not liquid water/liquid mixture.

Do not fake these calculations by reusing liquid formulas if the tube side is vapor/two-phase. If formulas are not validated, route the factory to explicit pending implementations and mark the missing method clearly.

Methods that must be reviewed per concrete case:

- `InitializeValues`
- `CalculateInitialAssumedDirtyOverallCoefficient`
- `TryCalculateTubePasses`
- `CalculateActualGeometry`
- `VerifyTubeVelocity`
- `CalculateTubeSideHeatTransferCoefficient`
- `CalculateTubeSidePressureDrop`
- `CalculateShellSideHeatTransferCoefficient`
- `CalculateShellSidePressureDrop`
- `VerifyAssumedDirtyOverallCoefficient`

## 4. Expand Calculation With TEMA

Goal: add TEMA construction checks after the Kern thermal/hydraulic design.

Known source:

- `C:\Discoduro\Trabajos Viejos\computador azul\Libros\TEMA\TEMA_9TH EDITION_2007.pdf`

Important finding:

- TEMA Section 7 gives basic thermal relations, but detailed shell-side/tube-side film coefficients and pressure losses are outside TEMA scope. Kern remains the source for thermal/hydraulic correlations.

Useful TEMA areas:

- Nomenclature and type designation: stationary head, shell type, rear head (`B-E-U`, `A-E-S`, etc.).
- Size numbering: shell ID and tube length.
- Specification sheet format.
- Tube pitch minimum: generally `1.25 * tube OD`.
- Triangular pattern caution when shell-side mechanical cleaning is required.
- Baffle spacing and support plate rules.
- U-tube bend and support considerations.
- Impingement protection.
- Flow-induced vibration review.

Recommended service:

- `TemaConstructionReviewService`

Expected output:

- Construction checks separate from thermal design checks.
- TEMA datasheet fields.
- TEMA type validation.
- Warnings such as "vibration review required" when appropriate.

## 5. Expand Calculation With Pressure Vessel Handbook / Megyesy

Goal: add mechanical/fabrication design after the thermal and TEMA construction design are stable.

Known sources:

- `C:\Discoduro\Trabajos Viejos\computador azul\Libros\Diseño de Equipos\Tanques\manual.de.recipientes.a.presion-megyes\manual de recipientes-megyesy.pdf`
- `C:\Discoduro\Trabajos Viejos\computador azul\Libros\Diseño de Equipos\Tanques\manual.de.recipientes.a.presion-megyes\pressure_vessel_handbook_megyesy.pdf`

Useful areas identified:

- Internal pressure vessel design.
- External pressure/vacuum.
- Cylindrical shell thickness.
- Heads/covers.
- Supports and saddles for horizontal vessels.
- Openings and nozzle reinforcement.
- Nozzle loads.
- Welding and joints.
- Corrosion allowance.
- Materials/specifications.
- Fabrication tolerances.
- Measures and weights.

Recommended service:

- `PressureVesselMechanicalDesignService`

Expected output:

- Mechanical calculation result.
- Fabrication sheet fields.
- Required shell/head/nozzle/support data.
- Clear distinction between process design and mechanical code review.

Important limitation:

- The Megyesy PDFs are scanned and OCR is weak. Use visual review/rendered pages for source reading.

## 6. Implement Factory For Reboiler

Goal: apply the same design architecture to reboilers.

Expected structure:

- Reboiler visual element implements the same design-capable interface.
- Reboiler-specific factory selects the correct concrete designer.
- Reboiler designs reuse common shell-and-tube geometry where valid.
- Reboiler-specific calculations override tube-side/shell-side boiling/recirculation behavior.

Known old C++ branches to revisit:

- `ValoresInicialesRehervidor`
- `CalcularRecirculacion`
- `CalcularCoeficienteTubosRehervidor`
- reboiler-specific Ud assumptions and stop criteria

Design pattern:

- Factory Method for designer selection.
- Template Method for calculation sequence.
- Strategy only where formulas become independently swappable and not just a one-off branch.

## 7. New Equipment: Falling Film Evaporator

Goal: implement a new falling film evaporator equipment from UI to design.

Scope:

- New visual element/equipment.
- Ports and stream contracts.
- Solver integration.
- Design tab/component.
- Design backend.
- Factory/concrete design implementations.
- SVG visual representation.
- Datasheet/report support.

Important:

- This should not be forced into the existing shell-and-tube exchanger if the behavior is materially different.
- Start with specifications and process cases before coding.

Likely design questions:

- Feed distribution.
- Film formation.
- Tube-side/shell-side allocation.
- Vapor/liquid separation.
- Residence time and wetting limits.
- Pressure drop and heat transfer correlations.

## 8. Plate Heat Exchanger Design

Goal: add design capability for plate heat exchangers in the future.

Current limitation:

- No local design reference has been provided yet.

Required next step:

- Research reliable sources before implementation.
- Prefer primary or authoritative sources: manufacturer engineering guides, standards, textbooks, or peer-reviewed material.

Potential scope:

- Plate geometry.
- Chevron angle/pattern.
- Heat transfer area.
- Pressure drop.
- Fouling.
- Gasket/material constraints.
- Number of plates/passes.
- Datasheet format.

Important:

- Do not reuse shell-and-tube assumptions for plate exchangers.
- Create a separate design factory and design variables when the domain requires it.

## Recommended Work Order

1. Finish shell-and-tube UI and prove recalculation works.
2. Validate E-101 condenser from UI.
3. Validate Enerquip/spec-sheet style example.
4. Complete missing shell-and-tube concrete classes from Kern/C++.
5. Add TEMA construction checks.
6. Build the final TEMA-style PDF datasheet after the pressure vessel mechanical module is integrated.
7. Add reboiler factory and design cases.
8. Specify and implement falling film evaporator.
9. Research and design plate exchanger module.
10. Add database persistence for equipment designs once the final saveable data shape is known.

## PDF Datasheet Timing

The printable datasheet/report must be implemented after item 5 is complete. The report needs the complete thermal design, TEMA construction review, and pressure vessel mechanical/fabrication data. Creating the PDF before that point would produce an attractive but incomplete equipment document.

Expected PDF style:

- Follow the TEMA specification sheet pattern.
- Include process, thermal, hydraulic, construction, and mechanical/fabrication sections.
- Include the exchanger SVG/sketch.
- Include units per value.
- Keep it compact and printable, closer to an industrial datasheet than to a generic app report.

## Database Persistence Timing

Design persistence must be one of the last implementation steps. The saveable shape of a design will keep growing while the UI, TEMA construction review, pressure vessel mechanical calculations, reboiler design, falling film evaporator, and plate exchanger modules are being defined.

Expected persistence direction:

- Persist user intent and user-defined `Variable<T>` values.
- Avoid saving calculated results as the primary truth.
- Recalculate equipment design results when the project/design is loaded.
- Keep persistence generic enough that new design variables do not require a database migration per variable.
- Add database persistence after the report/fabrication data model is stable enough to avoid churn.

## Guardrails

- Do not mix UI display logic with thermal equations.
- Do not mix Kern thermal calculations with TEMA construction checks.
- Do not mix pressure vessel mechanical code with shell-and-tube thermal design.
- Do not add fake formulas to make a case appear complete.
- Use `Variable<T>` for user-editable design variables.
- Preserve user-defined values during recalculation.
- Keep the initial design optimized enough to avoid obvious oversizing.
- Keep datasheets compact and industry-like.
- Add tests for calculated values, not manually injected expected intermediate state.
