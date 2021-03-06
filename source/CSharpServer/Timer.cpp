#include "stdafx.h"

#include "Timer.h"

namespace CSharpServer {

    void TimerEx::onTimer(bool canceled)
    {
        root->InternalOnTimer(canceled);
    }

    void TimerEx::onError(int error, const std::string& category, const std::string& message)
    {
        String^ cat = marshal_as<String^>(category);
        String^ msg = marshal_as<String^>(message);
        root->InternalOnError(errno, cat, msg);
    }

    Timer::Timer(CSharpServer::Service^ service) :
        _service(service),
        _timer(new std::shared_ptr<TimerEx>(std::make_shared<TimerEx>(service->_service.Value)))
    {
		_timer->get()->root = this;
    }

    Timer::Timer(CSharpServer::Service^ service, DateTime time) :
        _service(service),
        _timer(new std::shared_ptr<TimerEx>(std::make_shared<TimerEx>(service->_service.Value, CppCommon::UtcTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond))))
    {
		_timer->get()->root = this;
    }

    Timer::Timer(CSharpServer::Service^ service, TimeSpan timespan) :
        _service(service),
        _timer(new std::shared_ptr<TimerEx>(std::make_shared<TimerEx>(service->_service.Value, CppCommon::Timespan(timespan.Ticks * 100))))
    {
		_timer->get()->root = this;
    }

}
