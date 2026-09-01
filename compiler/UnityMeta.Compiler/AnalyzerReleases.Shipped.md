## Release 0.2.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
UMETA001 | UnityMeta | Error | Template methods must be static.
UMETA002 | UnityMeta | Error | Field get/set transform templates must return a value.
UMETA003 | UnityMeta | Error | Observer templates must return void.
UMETA004 | UnityMeta | Error | A template method must have exactly one template role.
UMETA005 | UnityMeta | Error | Every template parameter must have exactly one binding.
UMETA006 | UnityMeta | Error | Binding is invalid for the selected template kind.
UMETA007 | UnityMeta | Error | Template kind must match the containing aspect base class.
UMETA008 | UnityMeta | Error | Generic template methods are not supported by the current backend.
UMETA009 | UnityMeta | Error | Template methods must be public while direct-call weaving is used.
