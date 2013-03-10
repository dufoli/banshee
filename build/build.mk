include $(top_srcdir)/build/build.environment.mk

ALL_TARGETS = $(ASSEMBLY_FILE) theme-icons

if ENABLE_TESTS
    LINK += " $(NUNIT_LIBS)"
    ENABLE_TESTS_FLAG = "-define:ENABLE_TESTS"
endif

include $(top_srcdir)/build/build.dist.mk
include $(top_srcdir)/build/build.rules.mk

