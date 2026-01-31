# [Feature Name] Engineering Spec
Based on PRD: [Feature Name]_PRD.md version [PRD version]
Version: [e.g., 1.0]

## 0. Overview
[Provide a brief, high-level description of the feature's purpose, functionality, and integration with the existing system. Map directly to the PRD's requirements. Include the primary technology stack, architectural approach, and key non-functional requirements like performance, scalability, and security. Limit < 200 words.]
The [Feature Name] addresses [key problem from PRD] by [briefly describe main functionality]. It integrates with [list existing system components, e.g., database, API endpoints] using [technology/framework, default: .NET 8 with C#]. The architecture follows [pattern, e.g., MVVM or Clean Architecture] to ensure separation of concerns between [list main layers, e.g., presentation, business logic, data access]. Key strategies include [algorithms or approaches, e.g., caching for performance] to achieve [metrics, e.g., <100ms response time] while maintaining [qualities, e.g., high availability].

## 1. Traceability
### 1.1 Requirement ↔ Spec Index
| FR-### | Spec Items (S-###)  | Notes [brief]
|-------|---------------------|-------
| FR-001 | S-101, S-102 |
| FR-002 | S-110 |

### 1.2 Spec ↔ Acceptance Tests
| S-### | Acceptance Tests (AT-###) | Description [brief]
|-------|---------------------------|------------
| S-101 | AT-020, AT-021 | Ensure geometry is convex
| S-110 | AT-030 | Ensure clockwise winding in geometry

### 1.3 Open Questions
- SOQ-01 with proposed resolution options

## 2. Architecture
[Define the high-level architecture of the feature. Be brief, but cover all major components and their interactions.]
- Mermaid diagrams, internal modules, dependencies, failure domains.
- Public boundaries: services, adapters, storage, UI, messaging.

## 3. Data Models

### 3.1 Structures and Classes - essential fields only
[Define all major data structures and classes that represent domain concepts. Include properties, relationships, and serialization notes. Use C# code snippets for clarity, not full code. Align with PRD data requirements]
Example:
[Model Name, e.g., User]
public class User
{
    public int Id {get; set;}
    public string Username {get; set;}
}

### 3.2 interfaces
[Define all major interfaces]

## 4. Persistence
[Define the mechanism]
## 4.1 Persisted Data
- [All fields from User class]

## 5. Testing Strategy
[Describe overall approach. Default to unit tests in NUnit format. Ensure 80%+ coverage for critical paths. Include integration/end-to-end if needed.]

### 5.1 Unit Test Location
- Tests for source files in [Directory, e.g., Busi] are in a sibling folder named [Directory].UnitTests, e.g., Busi.UnitTests.
- Overall: Use Moq for mocks where possible, Assert for validations; cover exceptions and boundary conditions.

### 5.2 Acceptance Test Plan (AT-###)
- AT-020: <name>
  - Given …
  - When …
  - Then …
- AT-021: …

## 6. Implementation Considerations

### 6.1 [Platform/System] Integration
- *[How the feature integrates with existing APIs or systems]*
- *[Integration points and dependencies]*
- *[Compatibility requirements and constraints]*
- *[Guidelines or standards that must be followed]*

### 6.2 Scalability and Performance
- [e.g., Use async/await for I/O-bound ops; cache frequently accessed data with IMemoryCache.]
- [Metrics: Aim for <50ms per request; monitor with Application Insights.]

### 6.3 Dependencies and Risks
[List external libs, e.g., AutoMapper for DTOs; risks: Version conflicts – pin to specific versions.]
[Mitigations: Use dependency injection for loose coupling (S.O.L.I.D).]

### 6.4 User Experience
- *[UX principles and design decisions]*
- *[User feedback and interaction patterns]*
- *[Customization and configuration options]*
- *[Documentation and help features]*

## 7. Spec Items (S-###) – Implementation-Ready Requirements
- S-101: <title>
  - Desc: [brief description]
  - Maps: R-001
  - Interfaces: <types/methods>
  - Invariants: …
  - Edge cases: …
  - Acceptance: AT-020, AT-021

- S-110: <title>
  - Desc: [brief description]
  - Maps: R-002
  - …

## 8. Cross-Task Dependencies  
- Task ordering constraints
- Shared components that need coordination
- Integration points requiring careful sequencing

## 9. Implementation Context Budget
- Target: Spec created from this template is <20k tokens. If it exceeds this, break into multiple specs and have each reference the others.

## 10. Developer Guide
[The developer has only ever worked with C#, future AI sessions will implement all C# code, this section should walk the developer through what they need to do to implement the NON-C# parts of the feature.]
ie:
- [ ] [create database table named "Users" with columns "Id" and "Username"]
- [ ] [create 3D assets for the workbench and place them in file-x]

## 11. Spec Implementation / Update Linter Self-Check
- [ ] Traceability complete (R↔S, S↔AT)
- [ ] Interfaces concrete (signatures, types, exceptions)
- [ ] Ambiguities resolved or listed as OPEN-QUESTION
- [ ] Status updated to READY if there are no open questions, otherwise NEEDS-REVISION

**Status**: `READY` | `NEEDS-REVISION`