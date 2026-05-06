#ifndef USER_H
#define USER_H

#include <string>
#include <vector>

namespace app {

struct UserData {
    int id;
    std::string name;
    std::string email;
};

class UserService {
public:
    UserService();
    UserData findById(int id);
    std::vector<UserData> getAll();
    void save(UserData user);

private:
    std::vector<UserData> users_;
};

} // namespace app

#endif
