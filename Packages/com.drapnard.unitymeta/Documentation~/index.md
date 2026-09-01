# UnityMeta

UnityMeta is a metaprogramming/aspect authoring framework for Unity 2022.3.

The package exposes primitives rather than fixed gameplay features:

- `FieldGetAspectAttribute` + `[GetTemplate]` transforms ordinary loaded values;
- `FieldSetAspectAttribute` + `[SetTemplate]` transforms assigned values;
- `FieldChangeAspectAttribute` + `[ChangeTemplate]` observes real post-transform changes
  using `[OldValue]` and `[NewValue]`;
- `MethodAspectAttribute` + `[BeforeTemplate]` / `[AfterTemplate]` instruments methods;
- `[ReturnValue]` lets an after template observe a normal return value;
- metadata/target bindings let templates consume aspect constructor/named arguments,
  target names, instances, arguments, and sibling field values.

Import the package samples for Clamp, Read Transform, OnChange and method logging examples.

UnityMeta 0.x is experimental. Around/proceed, member introduction, property aspects, ref/address field interception, dependency graphs, exception-safe finally semantics and async state-machine
weaving are still roadmap work.
