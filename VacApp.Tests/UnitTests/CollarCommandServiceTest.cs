using Moq;
using VacApp_Bovinova_Platform.IoTMonitoring.Application.Internal.CommandServices;
using VacApp_Bovinova_Platform.IoTMonitoring.Domain.Model.Aggregates;
using VacApp_Bovinova_Platform.IoTMonitoring.Domain.Model.Commands;
using VacApp_Bovinova_Platform.IoTMonitoring.Domain.Repositories;
using VacApp_Bovinova_Platform.RanchManagement.Domain.Model.Aggregates;
using VacApp_Bovinova_Platform.RanchManagement.Domain.Model.Queries;
using VacApp_Bovinova_Platform.RanchManagement.Domain.Services;
using VacApp_Bovinova_Platform.Shared.Domain.Repositories;

namespace VacApp.Tests.UnitTests
{
    public class CollarCommandServiceTests
    {
        private const int OwnerId = 42;

        private static (CollarCommandService svc, Mock<ICollarRepository> repo) Build(int activeCollars)
        {
            var repo = new Mock<ICollarRepository>();
            repo.Setup(r => r.ExistsByDeviceIdAsync(It.IsAny<string>())).ReturnsAsync(false);
            repo.Setup(r => r.ExistsActiveByBovineIdAsync(It.IsAny<int>())).ReturnsAsync(false);
            repo.Setup(r => r.CountActiveByUserIdAsync(OwnerId)).ReturnsAsync(activeCollars);
            repo.Setup(r => r.AddAsync(It.IsAny<Collar>())).Returns(Task.CompletedTask);

            var bovineQuery = new Mock<IBovineQueryService>();
            bovineQuery.Setup(q => q.Handle(It.IsAny<GetBovinesByIdQuery>()))
                .ReturnsAsync(new Bovine("Ana", "female", new DateOnly(2020, 1, 1), "Holstein", 1, "img",
                    userId: OwnerId, minTemperature: 38.0, maxTemperature: 39.3, minHeartRate: 40, maxHeartRate: 80));

            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.CompleteAsync()).Returns(Task.CompletedTask);

            return (new CollarCommandService(repo.Object, bovineQuery.Object, uow.Object), repo);
        }

        private static RegisterCollarCommand Register() =>
            new(UserId: OwnerId, DeviceId: "collar-1-u42", BovineId: 7);

        [Fact]
        public async Task RegisterCollar_BelowAllowance_RegistersCollar()
        {
            var (svc, repo) = Build(activeCollars: Collar.MaxCollarsPerOwner - 1);

            var collar = await svc.Handle(Register());

            Assert.Equal("collar-1-u42", collar.DeviceId);
            repo.Verify(r => r.AddAsync(It.IsAny<Collar>()), Times.Once);
        }

        [Fact]
        public async Task RegisterCollar_AllowanceReached_ThrowsAndDoesNotRegister()
        {
            var (svc, repo) = Build(activeCollars: Collar.MaxCollarsPerOwner);

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.Handle(Register()));
            repo.Verify(r => r.AddAsync(It.IsAny<Collar>()), Times.Never);
        }
    }
}
