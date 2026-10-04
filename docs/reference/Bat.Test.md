# Bat.Test (test helper library)

`src/Test/Bat.Test` · namespace `Bat.Test` · depends on Bat.EntityFrameworkCore.Tools, Moq, NSubstitute, AutoFixture.AutoMoq,
AutoBogus, MockQueryable. **Not a test project** and contains no tests; consumers use it in their own unit tests.
For this repository's own tests see `tests/Bat.Regression.Tests` ([build-test-release.md](../build-test-release.md)).

- `TestTools` (entry point): `GetMock<T>()` → `MockBuilder<T>` (Setup/Throw/Verify fluent wrapper over Moq),
  `GetMockDbContext<TDb>()`, `GetMockUnitOfWork<TUow, TDb>()` (`IInitialUowBuilder` → mock repos with in-memory lists via MockQueryable),
  `GetNSubstitute<T>()`, `GetNSubstituteDbContext<TDb>()`, `GetService<TService>()` (`ServiceBuilder`: AutoFixture + AutoMoq,
  `Inject(mock)`, `Build()`), `ObjectFaker<T>()` (`GeneralBogusBuilder`).
- `ServiceMocker<TUow, TDb, TService>` / `EasyServiceMocker<TService>` — base classes for service tests with a mocked unit of work.
- Data fakers: `GeneralBogusBuilder<T>`, `EfBogusBuilder<TAggregate,TKey>`, `AggregateRootBogusBuilder<TAggregate,TKey>`, `EfBogusSeeder`.
- `StaticValuesBuilder` — temporarily set static properties and `Restore()` them.
- `TestExtensions.Randomizer<T>(Faker)`, `GenerateString(length)`.
- `GetNSubstituteUnitOfWork` throws `NotImplementedException`.

This project was not part of the 10.0.1 performance review beyond compilation.
