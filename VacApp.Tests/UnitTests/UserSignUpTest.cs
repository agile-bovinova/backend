using Moq;
using VacApp_Bovinova_Platform.IAM.Application.CommandServices;
using VacApp_Bovinova_Platform.IAM.Application.OutBoundServices;
using VacApp_Bovinova_Platform.IAM.Domain.Model.Aggregates;
using VacApp_Bovinova_Platform.IAM.Domain.Model.Commands;
using VacApp_Bovinova_Platform.IAM.Domain.Repositories;
using VacApp_Bovinova_Platform.Shared.Application.Outbound;
using VacApp_Bovinova_Platform.Shared.Domain.Repositories;

namespace VacApp.Tests.UnitTests
{
    public class UserSignUpTests
    {
        private static UserCommandService Build(Mock<IUnitOfWork> uow, Mock<ITokenService> tokens)
        {
            var users = new Mock<IUserRepository>();
            users.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
            users.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var hashing = new Mock<IHashingService>();
            hashing.Setup(h => h.GenerateHash(It.IsAny<string>())).Returns("hashed");

            return new UserCommandService(users.Object, uow.Object, hashing.Object, tokens.Object,
                new Mock<IPasswordResetTokenRepository>().Object, new Mock<IEmailSender>().Object);
        }

        [Fact]
        public async Task SignUp_WhenUserIsPersisted_ReturnsToken()
        {
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.CompleteAsync()).Returns(Task.CompletedTask);
            var tokens = new Mock<ITokenService>();
            tokens.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("jwt");

            var token = await Build(uow, tokens).Handle(new SignUpCommand("ana", "ana@test.com", "Demo1234!"));

            Assert.Equal("jwt", token);
        }

        [Fact]
        public async Task SignUp_WhenSaveFails_PropagatesErrorInsteadOfIssuingToken()
        {
            // Regression: a DB error (e.g. a NOT NULL column without default) used to be
            // swallowed, so the API answered 201 + a token for a user that was never saved.
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.CompleteAsync()).ThrowsAsync(new InvalidOperationException("db down"));
            var tokens = new Mock<ITokenService>();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Build(uow, tokens).Handle(new SignUpCommand("ana", "ana@test.com", "Demo1234!")));
            tokens.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
        }
    }
}
