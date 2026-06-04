#ifndef LOGGER_H
#define LOGGER_H

#include <string>

namespace app {
namespace utils {

class Logger {
public:
    Logger();
    void info(const std::string& message);
    void error(const std::string& message);

private:
    int logLevel_;
};

} // namespace utils
} // namespace app

#endif
