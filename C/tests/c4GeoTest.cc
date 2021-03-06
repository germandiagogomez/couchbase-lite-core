//
//  c4GeoTest.cc
//  Couchbase Lite Core
//
//  Created by Jens Alfke on 1/5/16.
//  Copyright (c) 2016 Couchbase. All rights reserved.
//

#include "c4Test.hh"
#include "c4View.h"
#include "c4DocEnumerator.h"
#include <iostream>
#include <algorithm>

static const char *kViewIndexPath = kTestDir "forest_temp.view.index";

static double randomLat() { return random() / (double)RAND_MAX * 180.0 - 90.0; }
static double randomLon() { return random() / (double)RAND_MAX * 360.0 - 180.0; }

class C4GeoTest : public C4Test {
public:

    C4View *view {nullptr};

    C4GeoTest(int testOption)
    :C4Test(testOption)
    {
        c4view_deleteByName(db, c4str("geoview"), nullptr);
        C4Error error;
        view = c4view_open(db, kC4SliceNull, c4str("geoview"), c4str("1"),
                           c4db_getConfig(db), &error);
        REQUIRE(view);
    }

    ~C4GeoTest() {
        C4Error error;
        if (view && !c4view_delete(view, &error)) {
            char msg[256];
            WarnError("Failed to delete c4View: error %d/%d: %s\n",
                      error.domain, error.code, c4error_getMessageC(error, msg, sizeof(msg)));
            FAIL();
        }
        c4view_free(view);
    }


    void createDocs(unsigned n, bool verbose =false) {
        srandom(42);
        TransactionHelper t(db);

        for (unsigned i = 0; i < n; ++i) {
            char docID[20];
            sprintf(docID, "%u", i);

            double lat0 = randomLat(), lon0 = randomLon();
            double lat1 = std::min(lat0 + 0.5, 90.0), lon1 = std::min(lon0 + 0.5, 180.0);
            char body[1000];
            sprintf(body, "(%g, %g, %g, %g)", lon0, lat0, lon1, lat1);

            C4DocPutRequest rq = {};
            rq.docID = c4str(docID);
            rq.body = c4str(body);
            rq.save = true;
            C4Error error;
            C4Document *doc = c4doc_put(db, &rq, nullptr, &error);
            REQUIRE(doc != nullptr);
            if (verbose)
                Log("Added %s --> %s\n", docID, body);
            c4doc_free(doc);
        }
    }

    void createIndex() {
        C4Error error;
        C4Indexer* ind = c4indexer_begin(db, &view, 1, &error);
        REQUIRE(ind);

        C4DocEnumerator* e = c4indexer_enumerateDocuments(ind, &error);
        REQUIRE(e);

        C4Document *doc;
        while (nullptr != (doc = c4enum_nextDocument(e, &error))) {
            char body [1000];
            memcpy(body, doc->selectedRev.body.buf, doc->selectedRev.body.size);
            body[doc->selectedRev.body.size] = '\0';

            C4GeoArea area;
            REQUIRE(sscanf(body, "(%lf, %lf, %lf, %lf)",
                           &area.xmin, &area.ymin, &area.xmax, &area.ymax) == 4);

            C4Key *keys[1];
            C4Slice values[1];
            keys[0] = c4key_newGeoJSON(c4str("{\"geo\":true}"), area);
            values[0] = c4str("1234");
            REQUIRE(c4indexer_emit(ind, doc, 0, 1, keys, values, &error));
            c4key_free(keys[0]);
            c4doc_free(doc);
        }
        c4enum_free(e);
        REQUIRE(error.code == 0);
        REQUIRE(c4indexer_end(ind, true, &error));
    }
};


N_WAY_TEST_CASE_METHOD(C4GeoTest, "Geo CreateIndex", "[Geo][View][C]") {
    createDocs(100);
    createIndex();
}


N_WAY_TEST_CASE_METHOD(C4GeoTest, "Geo Query", "[Geo][View][C]") {
    static const bool verbose = false;
    createDocs(100, verbose);
    createIndex();

    C4GeoArea queryArea = {10, 10, 40, 40};
    C4Error error;
    C4QueryEnumerator* e = c4view_geoQuery(view, queryArea, &error);
    REQUIRE(e);

    unsigned found = 0;
    while (c4queryenum_next(e, &error)) {
        ++found;
        C4GeoArea a = e->geoBBox;
        if (verbose) {
            Log("Found doc %.*s : (%g, %g)--(%g, %g)\n",
                (int)e->docID.size, (char*)e->docID.buf, a.xmin, a.ymin, a.xmax, a.ymax);
        }

        C4Slice expected = C4STR("1234");
        REQUIRE(e->value == expected);
        REQUIRE(a.xmin <= 40);
        REQUIRE(a.xmax >= 10);
        REQUIRE(a.ymin <= 40);
        REQUIRE(a.ymax >= 10);

        expected = C4STR("{\"geo\":true}");
        REQUIRE(e->geoJSON == expected);
    }
    c4queryenum_free(e);
    REQUIRE(error.code == 0);
    REQUIRE(found == 2u);
}

/*

class C4SQLiteGeoTest : public C4GeoTest {

    virtual const char* storageType() const override     {return kC4SQLiteStorageEngine;}

    CPPUNIT_TEST_SUB_SUITE( C4SQLiteGeoTest, C4GeoTest );
    CPPUNIT_TEST_SUITE_END();
};

CPPUNIT_TEST_SUITE_REGISTRATION(C4SQLiteGeoTest);
*/
