#include "user.h"
#include <algorithm>
#include <stdexcept>

namespace app {

UserService::UserService() {}

UserData UserService::findById(int id) {
    for (auto& u : users_) {
        if (u.id == id) {
            return u;
        }
    }
    throw std::runtime_error("User not found");
}

std::vector<UserData> UserService::getAll() {
    return users_;
}

void UserService::save(UserData user) {
    users_.push_back(user);
}

} // namespace app
