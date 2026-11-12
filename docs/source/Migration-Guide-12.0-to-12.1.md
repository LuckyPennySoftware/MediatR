# Migration guide: 12.0 to 12.1

With 12.1, `AddMediatR` no longer scans for any implementations of:
- Behaviors
- Stream behaviors
- Pre-processors
- Post-processors

You will need to explicitly register these with methods:
- Behaviors
  - `AddBehavior`
  - `AddOpenBehavior`
- Stream behaviors
  - `AddStreamBehavior`
  - `AddOpenStreamBehavior`
- Pre processors
  - `AddRequestPreProcessor`
  - `AddOpenRequestPreProcessor`
- Post processors
  - `AddRequestPostProcessor`
  - `AddOpenRequestPostProcessor`

Since you couldn't change the order of these with the scanning approach, `AddMediatR` now registers with `ServiceCollection` in the exact order added to each list of registrations.
