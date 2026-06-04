#include "logger.h"
#include <iostream>

namespace app {
namespace utils {

Logger::Logger() : logLevel_(0) {}

void Logger::info(const std::string& message) {
    std::cout << "[INFO] " << message << std::endl;
}

void Logger::error(const std::string& message) {
    std::cerr << "[ERROR] " << message << std::endl;
}

} // namespace utils
} // namespace app
