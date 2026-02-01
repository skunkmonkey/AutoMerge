# Real World Merge Conflict Testing Files

This directory contains realistic source files designed to test AutoMerge with actual Git merge conflicts in SourceTree.

## Testing Workflow

1. **Commit these files to base branch**
   ```bash
   git checkout -b test-base
   git add tests/RealWorldTests/
   git commit -m "Add real world test files"
   ```

2. **Create feature branch A with changes**
   ```bash
   git checkout -b feature-a test-base
   # Make changes to files (see scenarios below)
   git commit -am "Feature A changes"
   ```

3. **Create feature branch B with conflicting changes**
   ```bash
   git checkout test-base
   git checkout -b feature-b
   # Make different changes to same areas (see scenarios below)
   git commit -am "Feature B changes"
   ```

4. **Merge and resolve with AutoMerge**
   ```bash
   git checkout test-base
   git merge feature-a  # Should succeed
   git merge feature-b  # Will create conflicts
   # Use SourceTree's merge tool (configured to use AutoMerge) to resolve
   ```

## Conflict Scenarios

### Easy: OrderConfiguration.cs
**Scenario:** Two developers add different configuration properties to the same section.

**Branch A adds:**
- `MaxRetryAttempts` property
- `RetryDelaySeconds` property

**Branch B adds:**
- `EnableNotifications` property  
- `NotificationEmail` property

**Expected resolution:** Keep both sets of properties. No logical interference, just textual overlap.

---

### Medium: OrderProcessor.cs
**Scenario:** Two developers add features that interact with shared state.

**Branch A adds:**
- `ValidateInventory()` method that checks stock levels
- Call to `ValidateInventory()` in `ProcessOrder()`

**Branch B adds:**
- `CalculateShippingCost()` method that uses order total
- Call to `CalculateShippingCost()` in `ProcessOrder()`

**Expected resolution:** Both methods should be added, and both should be called in `ProcessOrder()` in the correct order (validate inventory first, then calculate shipping).

---

### Hard: PaymentService.cs
**Scenario:** Two developers refactor the same payment processing logic differently.

**Branch A refactors:**
- Extracts validation logic into separate `ValidatePaymentMethod()` method
- Adds new credit card validation rules
- Restructures error handling with specific exceptions

**Branch B refactors:**
- Extracts validation logic into `ValidateTransaction()` method (different approach)
- Adds fraud detection checks
- Restructures with a validation result pattern instead of exceptions

**Expected resolution:** AI must understand both intents and merge them into a cohesive solution that:
- Combines both validation approaches appropriately
- Preserves credit card validation AND fraud detection
- Uses a consistent error handling pattern

---

## Usage Tips

- Start with the Easy scenario to verify AutoMerge setup works
- Test each scenario in isolation (use different base branches if needed)
- The Hard scenario may require AI guidance to properly merge the refactorings
- These files are intentionally simple to keep conflicts focused and testable
