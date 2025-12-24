# For use Bat.Test just do it :

## 1- Install Bat.Test on your project


## 2- Use it in UnitTests
for example :

    ```csharp
    [Fact]
    public async Task Add_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var userModel = TestTools.ObjectFaker<UserModel>()
            .WithNaturalInt()
            .Generate();

        DbContext.SetDbSet(x => x.Users)
            .SetAsyncSaveChange(true);

        // Act
        var result = await Service.Add(userModel, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
    ```


    ```
    [Fact]
    public async Task Update_UserNotFound_ReturnErrorMessage()
    {
        // Arrange
        var userId = GlobalVariables.UserId;
        var invalidUserId = Guid.NewGuid();
        var users = TestTools.ObjectFaker<User>()
            .WithNaturalInt()
            .Set(x => x.UserId, userId)
            .Generate(1);

        var userModel = TestTools.ObjectFaker<UserModel>()
            .WithNaturalInt()
            .Set(x => x.UserId, invalidUserId)
            .Generate();

        DbContext.SetDbSet(x => x.Users, users); 

        // Act
        var result = await Service.Update(userModel, default);

        // Assert 
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ServiceMessage.UserNotExist);
    }
    ```
